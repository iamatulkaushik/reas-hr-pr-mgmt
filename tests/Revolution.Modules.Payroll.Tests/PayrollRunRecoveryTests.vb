Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class PayrollRunRecoveryTests
    Private ReadOnly _runs As New FakeRunRepository()
    Private ReadOnly _ledger As New FakeRecoveryLedger()
    Private ReadOnly _approvals As New FakeApprovals()
    Private ReadOnly _perms As New FakePayPermissions()
    Private ReadOnly _audit As New FakePayAudit()
    Private ReadOnly _service As PayrollRunService

    Public Sub New()
        For Each action As PermissionAction In [Enum].GetValues(GetType(PermissionAction))
            _perms.Allowed.Add(action)
        Next
        _service = New PayrollRunService(_runs, New FakeSalaryRepository(), New FakeRates(), _approvals, _perms, New FakePayEntitlements(), _audit, New FakePaySession(), Nothing, Nothing, _ledger)
        _runs.Inputs.Add(RunTestData.Facts(101, "E101", paidDays:=30D))
        _runs.Inputs.Add(RunTestData.Facts(102, "E102", paidDays:=30D))
    End Sub

    Private Function Draft() As Integer
        Return _runs.AddRun(PayrollRunStatus.Draft)
    End Function

    <Fact>
    Public Sub Process_TakesTheClaimAsAnOtherDeduction_AndHoldsIt()
        _ledger.DueRows.Add(New ArrearsRecoveryDue(7, 102, "E102", 1500D))
        Dim id As Integer = Draft()
        Assert.True(_service.Process(id).Succeeded)
        Dim lines As List(Of RunLineResult) = _runs.Lines(id)
        Assert.Equal(0D, lines.Single(Function(l) l.EmployeeId = 101).OtherDeductions)
        Dim line As RunLineResult = lines.Single(Function(l) l.EmployeeId = 102)
        Assert.Equal(1500D, line.OtherDeductions)
        Assert.True(line.TotalDeductions >= 1500D)
        Assert.Equal(New List(Of Integer) From {7}, _ledger.Held(id))
    End Sub

    <Fact>
    Public Sub Process_RecoveryLowersNetPayByTheAmount()
        Dim plainId As Integer = Draft()
        Assert.True(_service.Process(plainId).Succeeded)
        Dim plainNet As Decimal = _runs.Lines(plainId).Single(Function(l) l.EmployeeId = 102).NetPay
        _ledger.DueRows.Add(New ArrearsRecoveryDue(7, 102, "E102", 1500D))
        Dim id As Integer = Draft()
        Assert.True(_service.Process(id).Succeeded)
        Assert.Equal(plainNet - 1500D, _runs.Lines(id).Single(Function(l) l.EmployeeId = 102).NetPay)
    End Sub

    <Fact>
    Public Sub Process_NoClaims_HoldsNothing()
        Dim id As Integer = Draft()
        Assert.True(_service.Process(id).Succeeded)
        Assert.Empty(_ledger.Held)
        Assert.Empty(_service.RecoveryNotes)
    End Sub

    <Fact>
    Public Sub Process_EmployeeNotInTheRun_IsNamedAndNotHeld()
        _ledger.DueRows.Add(New ArrearsRecoveryDue(8, 999, "E999", 300D))
        Dim id As Integer = Draft()
        Assert.True(_service.Process(id).Succeeded)
        Assert.Equal(New List(Of String) From {"E999"}, _service.RecoveryNotes)
        Assert.Empty(_ledger.Held)
    End Sub

    <Fact>
    Public Sub Process_WhenAnEmployeeFails_NothingIsHeld()
        _runs.Inputs.Add(RunTestData.Facts(103, "E103", withSalary:=False))
        _ledger.DueRows.Add(New ArrearsRecoveryDue(7, 102, "E102", 1500D))
        Dim id As Integer = Draft()
        Assert.False(_service.Process(id).Succeeded)
        Assert.Empty(_ledger.Held)
        Assert.False(_runs.Lines.ContainsKey(id))
    End Sub

    <Fact>
    Public Sub Process_ClaimTakenByAnotherRun_FailsWithTheMessage_AndSavesNoLines()
        _ledger.DueRows.Add(New ArrearsRecoveryDue(7, 102, "E102", 1500D))
        _ledger.FailReserve = "A claim is not open for recovery. Process the run again."
        Dim id As Integer = Draft()
        Dim r = _service.Process(id)
        Assert.False(r.Succeeded)
        Assert.Equal("A claim is not open for recovery. Process the run again.", r.Message)
        Assert.False(_runs.Lines.ContainsKey(id))
        Assert.Equal(PayrollRunStatus.Draft, _runs.StatusOf(id))
    End Sub

    <Fact>
    Public Sub Process_CannotReadTheClaims_Fails()
        _ledger.FailDue = "Database is busy."
        Dim id As Integer = Draft()
        Dim r = _service.Process(id)
        Assert.False(r.Succeeded)
        Assert.Equal("Database is busy.", r.Message)
        Assert.False(_runs.Lines.ContainsKey(id))
    End Sub

    <Fact>
    Public Sub Process_Twice_HoldsTheSameClaimsAgain()
        _ledger.DueRows.Add(New ArrearsRecoveryDue(7, 102, "E102", 1500D))
        Dim id As Integer = Draft()
        Assert.True(_service.Process(id).Succeeded)
        Assert.True(_service.Process(id).Succeeded)
        Assert.Equal(New List(Of Integer) From {7}, _ledger.Held(id))
        Assert.Equal(1500D, _runs.Lines(id).Single(Function(l) l.EmployeeId = 102).OtherDeductions)
    End Sub

    <Fact>
    Public Sub ReturnToDraft_ReleasesTheClaims()
        _ledger.DueRows.Add(New ArrearsRecoveryDue(7, 102, "E102", 1500D))
        Dim id As Integer = Draft()
        Assert.True(_service.Process(id).Succeeded)
        Assert.True(_service.ReturnToDraft(id, "Check again.").Succeeded)
        Assert.Contains(id, _ledger.ReleasedRuns)
        Assert.Empty(_ledger.Held)
    End Sub

    <Fact>
    Public Sub ReturnToDraft_ReleaseFails_SaysSo_ButTheRunIsInDraft()
        _ledger.DueRows.Add(New ArrearsRecoveryDue(7, 102, "E102", 1500D))
        Dim id As Integer = Draft()
        Assert.True(_service.Process(id).Succeeded)
        _ledger.FailRelease = "Database is busy."
        Dim r = _service.ReturnToDraft(id, Nothing)
        Assert.False(r.Succeeded)
        Assert.Contains("arrears recoveries were not released", r.Message, StringComparison.Ordinal)
        Assert.Equal(PayrollRunStatus.Draft, _runs.StatusOf(id))
    End Sub

    <Fact>
    Public Sub Lock_KeepsTheClaimsHeld()
        _ledger.DueRows.Add(New ArrearsRecoveryDue(7, 102, "E102", 1500D))
        Dim id As Integer = Draft()
        Assert.True(_service.Process(id).Succeeded)
        Assert.True(_service.Approve(id).Succeeded)
        Assert.True(_service.Lock(id).Succeeded)
        Assert.Empty(_ledger.ReleasedRuns)
        Assert.Equal(New List(Of Integer) From {7}, _ledger.Held(id))
    End Sub

    <Fact>
    Public Sub Reopen_ReleasesTheClaims()
        _ledger.DueRows.Add(New ArrearsRecoveryDue(7, 102, "E102", 1500D))
        Dim id As Integer = Draft()
        Assert.True(_service.Process(id).Succeeded)
        Assert.True(_service.Approve(id).Succeeded)
        Assert.True(_service.Lock(id).Succeeded)
        Assert.True(_service.Reopen(id, "Wrong attendance in the month.").Succeeded)
        Assert.Contains(id, _ledger.ReleasedRuns)
        Assert.Empty(_ledger.Held)
    End Sub

    <Fact>
    Public Sub Submit_ApprovalFails_ReleasesTheClaims()
        _approvals.Required = True
        _approvals.SubmitFailure = "Approval service is down."
        _ledger.DueRows.Add(New ArrearsRecoveryDue(7, 102, "E102", 1500D))
        Dim id As Integer = Draft()
        Assert.True(_service.Process(id).Succeeded)
        Assert.False(_service.Submit(id).Succeeded)
        Assert.Equal(PayrollRunStatus.Draft, _runs.StatusOf(id))
        Assert.Contains(id, _ledger.ReleasedRuns)
    End Sub

    <Fact>
    Public Sub WithoutALedger_ARunTakesNoRecovery()
        Dim plain As New PayrollRunService(_runs, New FakeSalaryRepository(), New FakeRates(), _approvals, _perms, New FakePayEntitlements(), _audit, New FakePaySession())
        Dim id As Integer = Draft()
        Assert.True(plain.Process(id).Succeeded)
        Assert.All(_runs.Lines(id), Sub(l) Assert.Equal(0D, l.OtherDeductions))
        Assert.True(plain.ReturnToDraft(id, Nothing).Succeeded)
    End Sub
End Class
