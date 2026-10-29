Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class PayslipServiceTests
    Private NotInheritable Class FakePayslipRepository
        Implements IPayslipRepository

        Public ReadOnly Rows As New List(Of PayslipRow)()
        Public Failure As String

        Public Function GetPayslipRows(runId As Integer, employeeId As Integer?) As IReadOnlyList(Of PayslipRow) Implements IPayslipRepository.GetPayslipRows
            If Failure IsNot Nothing Then Throw New PayrollRuleException(Failure, 50311, Nothing)
            Return Rows.Where(Function(r) Not employeeId.HasValue OrElse r.EmployeeId = employeeId.Value).ToList()
        End Function
    End Class

    Private NotInheritable Class FakeReleaser
        Implements IPayslipIdReleaser

        Public Failure As String
        Public Calls As Integer
        Public LastIds As IReadOnlyCollection(Of Integer)

        Public Function ReleaseForPayslips(employeeIds As IReadOnlyCollection(Of Integer)) As OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)) Implements IPayslipIdReleaser.ReleaseForPayslips
            Calls += 1
            LastIds = employeeIds
            If Failure IsNot Nothing Then Return OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)).Fail(Failure)
            Dim d As New Dictionary(Of Integer, ReleasedIds)()
            For Each id As Integer In employeeIds
                d(id) = New ReleasedIds With {.Uan = "100123456789", .EsiIp = "1234567890", .AccountNumber = "111122223333"}
            Next
            Return OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)).Ok(d)
        End Function
    End Class

    Private ReadOnly _repo As New FakePayslipRepository()
    Private ReadOnly _releaser As New FakeReleaser()
    Private ReadOnly _perms As New FakePayPermissions()
    Private ReadOnly _plan As New FakePayEntitlements()
    Private ReadOnly _audit As New FakePayAudit()
    Private ReadOnly _service As PayslipService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _service = New PayslipService(_repo, _releaser, _perms, _plan, _audit)
    End Sub

    Private Shared Function Row(employeeId As Integer, status As PayrollRunStatus, Optional netPay As Decimal = 14270D) As PayslipRow
        Return New PayslipRow With {.RunId = 5, .RunStatus = status, .EmployeeId = employeeId, .EmpCode = "E" & employeeId.ToString(),
                                    .UanLast4 = "6789", .EsiIpLast4 = "7890", .AccountLast4 = "3333", .NetPay = netPay, .GrossEarnings = 16000D}
    End Function

    <Fact>
    Public Sub Default_MasksIds_AndNeverCallsTheReleaser()
        _repo.Rows.Add(Row(101, PayrollRunStatus.Approved))
        Dim r = _service.GetPayslips(5, Nothing, False)
        Assert.True(r.Succeeded)
        Dim ids As PayslipIds = r.Value(0).Ids
        Assert.True(ids.IsMasked)
        Assert.Equal("XXXXXXXX6789", ids.Uan)
        Assert.Equal("XXXXXXXX7890", ids.EsiIp)
        Assert.Equal("XXXXXXXX3333", ids.AccountNumber)
        Assert.Equal(0, _releaser.Calls)
    End Sub

    <Fact>
    Public Sub FullIds_ApprovedRun_ReleasesPlainIds()
        _repo.Rows.Add(Row(101, PayrollRunStatus.Locked))
        _repo.Rows.Add(Row(102, PayrollRunStatus.Locked))
        Dim r = _service.GetPayslips(5, Nothing, True)
        Assert.True(r.Succeeded)
        Assert.False(r.Value(0).Ids.IsMasked)
        Assert.Equal("100123456789", r.Value(0).Ids.Uan)
        Assert.Equal("111122223333", r.Value(1).Ids.AccountNumber)
        Assert.Equal(1, _releaser.Calls)
        Assert.Equal(2, _releaser.LastIds.Count)
    End Sub

    <Fact>
    Public Sub FullIds_DraftRun_IsRefused_WithoutRelease()
        _repo.Rows.Add(Row(101, PayrollRunStatus.Processed))
        Dim r = _service.GetPayslips(5, Nothing, True)
        Assert.False(r.Succeeded)
        Assert.Equal(0, _releaser.Calls)
    End Sub

    <Fact>
    Public Sub FullIds_ReleaserRefuses_NoPayslipsAndNoAudit()
        _repo.Rows.Add(Row(101, PayrollRunStatus.Approved))
        _releaser.Failure = "You are not allowed to do this."
        Dim r = _service.GetPayslips(5, Nothing, True)
        Assert.False(r.Succeeded)
        Assert.Equal("You are not allowed to do this.", r.Message)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub DraftRun_IsMarkedDraft_ApprovedIsNot()
        _repo.Rows.Add(Row(101, PayrollRunStatus.Submitted))
        Assert.True(_service.GetPayslips(5, Nothing, False).Value(0).IsDraft)
        _repo.Rows.Clear()
        _repo.Rows.Add(Row(101, PayrollRunStatus.Approved))
        Assert.False(_service.GetPayslips(5, Nothing, False).Value(0).IsDraft)
    End Sub

    <Fact>
    Public Sub NetPayInWords_Filled()
        _repo.Rows.Add(Row(101, PayrollRunStatus.Approved, 14270D))
        Assert.Equal("Rupees Fourteen Thousand Two Hundred Seventy Only", _service.GetPayslips(5, Nothing, False).Value(0).NetPayInWords)
    End Sub

    <Fact>
    Public Sub OneEmployee_ReturnsOnlyThatPayslip()
        _repo.Rows.Add(Row(101, PayrollRunStatus.Approved))
        _repo.Rows.Add(Row(102, PayrollRunStatus.Approved))
        Dim r = _service.GetPayslips(5, 102, False)
        Assert.Equal(1, r.Value.Count)
        Assert.Equal(102, r.Value(0).Row.EmployeeId)
    End Sub

    <Fact>
    Public Sub MissingIds_MaskToEmpty()
        Dim row0 As PayslipRow = Row(101, PayrollRunStatus.Approved)
        row0.UanLast4 = String.Empty
        row0.AccountLast4 = String.Empty
        _repo.Rows.Add(row0)
        Dim ids As PayslipIds = _service.GetPayslips(5, Nothing, False).Value(0).Ids
        Assert.Equal(String.Empty, ids.Uan)
        Assert.Equal(String.Empty, ids.AccountNumber)
        Assert.Equal("XXXXXXXX7890", ids.EsiIp)
    End Sub

    <Fact>
    Public Sub Audit_HoldsCountAndFlag_NoIdsNoAmounts()
        _repo.Rows.Add(Row(101, PayrollRunStatus.Approved))
        _service.GetPayslips(5, Nothing, True)
        Dim entry As AuditEntry = _audit.Entries(0)
        Assert.Equal("Payslip.Print", entry.Action)
        Assert.Equal("5", entry.EntityId)
        Assert.Contains("1 payslips, full ids yes", entry.Details)
        Assert.DoesNotContain("100123456789", entry.Details)
        Assert.DoesNotContain("14270", entry.Details)
    End Sub

    <Fact>
    Public Sub UnknownRun_ShowsDatabaseMessage()
        _repo.Failure = "Run not found."
        Dim r = _service.GetPayslips(5, Nothing, False)
        Assert.False(r.Succeeded)
        Assert.Equal("Run not found.", r.Message)
    End Sub

    <Fact>
    Public Sub NoRows_IsRefused()
        Assert.False(_service.GetPayslips(5, Nothing, False).Succeeded)
    End Sub

    <Fact>
    Public Sub WithoutView_IsRefused()
        _repo.Rows.Add(Row(101, PayrollRunStatus.Approved))
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.GetPayslips(5, Nothing, False).Succeeded)
    End Sub

    <Fact>
    Public Sub PlanWithoutPayslips_IsRefused()
        _repo.Rows.Add(Row(101, PayrollRunStatus.Approved))
        _plan.Disabled.Add(PlanFeature.Payslips)
        Assert.False(_service.GetPayslips(5, Nothing, False).Succeeded)
    End Sub

    <Fact>
    Public Sub BadArguments_AreRefused()
        Assert.False(_service.GetPayslips(0, Nothing, False).Succeeded)
        Assert.False(_service.GetPayslips(5, 0, False).Succeeded)
    End Sub

    <Fact>
    Public Sub AuditFailure_IsReported()
        _repo.Rows.Add(Row(101, PayrollRunStatus.Approved))
        _audit.Fail = True
        Assert.False(_service.GetPayslips(5, Nothing, False).Succeeded)
    End Sub

    <Fact>
    Public Sub ShowTds_FollowsThePlan()
        _repo.Rows.Add(Row(101, PayrollRunStatus.Approved))
        Assert.True(_service.GetPayslips(5, Nothing, False).Value(0).ShowTds)
        _plan.Disabled.Add(PlanFeature.Tds)
        Assert.False(_service.GetPayslips(5, Nothing, False).Value(0).ShowTds)
    End Sub

    <Fact>
    Public Sub TdsAmount_TravelsOnTheRow()
        Dim r As PayslipRow = Row(101, PayrollRunStatus.Approved)
        r.TdsAmount = 1000D
        _repo.Rows.Add(r)
        Assert.Equal(1000D, _service.GetPayslips(5, Nothing, False).Value(0).Row.TdsAmount)
    End Sub
End Class
