Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Overpaid arrears claims are recovered through the regular run (Owner, 2026-11-03): taken as other deductions at Process, marked recovered at Lock, put back at Reopen.</summary>
Public Class ArrearsRecoveryRunTests
    Private NotInheritable Class FakeRecoveryStore
        Implements IArrearsRecoveryStore

        Public ReadOnly Pending As New Dictionary(Of Integer, Decimal)()
        Public ReadOnly Applied As New List(Of Integer)()
        Public ReadOnly Reversed As New List(Of Integer)()
        Public ApplyFails As String
        Public ReverseFails As String

        Public Function PendingByEmployee() As IReadOnlyDictionary(Of Integer, Decimal) Implements IArrearsRecoveryStore.PendingByEmployee
            Return Pending
        End Function

        Public Sub Apply(runId As Integer) Implements IArrearsRecoveryStore.Apply
            If ApplyFails IsNot Nothing Then Throw New PayrollRuleException(ApplyFails)
            Applied.Add(runId)
        End Sub

        Public Sub Reverse(runId As Integer) Implements IArrearsRecoveryStore.Reverse
            If ReverseFails IsNot Nothing Then Throw New PayrollRuleException(ReverseFails)
            Reversed.Add(runId)
        End Sub
    End Class

    Private ReadOnly _runs As New FakeRunRepository()
    Private ReadOnly _store As New FakeRecoveryStore()
    Private ReadOnly _service As PayrollRunService

    Public Sub New()
        Dim perms As New FakePayPermissions()
        For Each action As PermissionAction In [Enum].GetValues(GetType(PermissionAction))
            perms.Allowed.Add(action)
        Next
        _service = New PayrollRunService(_runs, New FakeSalaryRepository(), New FakeRates(), New FakeApprovals(), perms, New FakePayEntitlements(), New FakePayAudit(),
                                         New FakePaySession(), Nothing, Nothing, _store)
    End Sub

    Private Function DraftRun() As Integer
        _runs.Inputs.Add(RunTestData.Facts(101, "E101", paidDays:=30D))
        _runs.Inputs.Add(RunTestData.Facts(102, "E102", paidDays:=30D))
        Return _runs.AddRun(PayrollRunStatus.Draft)
    End Function

    <Fact>
    Public Sub Process_TakesWhatTheEmployeeOwes_AsOtherDeductions()
        Dim id As Integer = DraftRun()
        _store.Pending(101) = 500D
        Assert.True(_service.Process(id).Succeeded)
        Dim lines As List(Of RunLineResult) = _runs.Lines(id)
        Assert.Equal(500D, lines.Single(Function(l) l.EmployeeId = 101).OtherDeductions)
        Assert.Equal(0D, lines.Single(Function(l) l.EmployeeId = 102).OtherDeductions)
    End Sub

    <Fact>
    Public Sub Process_NothingOwed_LeavesOtherDeductionsAtZero()
        Dim id As Integer = DraftRun()
        Assert.True(_service.Process(id).Succeeded)
        Assert.All(_runs.Lines(id), Sub(l) Assert.Equal(0D, l.OtherDeductions))
    End Sub

    <Fact>
    Public Sub Lock_MarksTheRecoveries()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Approved)
        _runs.Lines(id) = New List(Of RunLineResult) From {RunTestData.Line(101, 0D)}
        Assert.True(_service.Lock(id).Succeeded)
        Assert.Equal(id, _store.Applied.Single())
    End Sub

    <Fact>
    Public Sub Lock_WhenTheRecoveriesNoLongerMatch_RunStaysLocked_AndSaysSo()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Approved)
        _runs.Lines(id) = New List(Of RunLineResult) From {RunTestData.Line(101, 0D)}
        _store.ApplyFails = "The arrears recoveries in the run do not match the open recovery claims. Process the run again."
        Dim r As OperationResult = _service.Lock(id)
        Assert.False(r.Succeeded)
        Assert.Contains("arrears recoveries were not saved", r.Message, StringComparison.Ordinal)
        Assert.Equal(PayrollRunStatus.Locked, _runs.StatusOf(id))
    End Sub

    <Fact>
    Public Sub ApplyLoanRecoveries_RetriesTheArrearsRecoveries()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Locked)
        _runs.Lines(id) = New List(Of RunLineResult) From {RunTestData.Line(101, 0D)}
        Assert.True(_service.ApplyLoanRecoveries(id).Succeeded)
        Assert.Equal(id, _store.Applied.Single())
    End Sub

    <Fact>
    Public Sub Reopen_PutsTheRecoveriesBack()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Locked)
        Assert.True(_service.Reopen(id, "Wrong attendance for two employees").Succeeded)
        Assert.Equal(id, _store.Reversed.Single())
    End Sub

    <Fact>
    Public Sub Reopen_WhenTheyCannotBePutBack_SaysSo()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Locked)
        _store.ReverseFails = "Database error."
        Dim r As OperationResult = _service.Reopen(id, "Wrong attendance for two employees")
        Assert.False(r.Succeeded)
        Assert.Contains("arrears recoveries were not reversed", r.Message, StringComparison.Ordinal)
    End Sub
End Class
