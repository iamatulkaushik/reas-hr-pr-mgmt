Imports Revolution.Core
Imports Revolution.Modules.Arrears
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class ArrearsRunServiceTests
    Private ReadOnly _repo As New FakeArrearsRepository()
    Private ReadOnly _source As New FakeArrearsSource()
    Private ReadOnly _runs As New FakeArrearsRunRepository()
    Private ReadOnly _perms As New FakeArrearsPermissions()
    Private ReadOnly _plan As New FakeArrearsEntitlements()
    Private ReadOnly _audit As New FakeArrearsAudit()
    Private ReadOnly _service As ArrearsRunService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.Create)
        _service = New ArrearsRunService(_repo, _source, _runs, New FakeArrearsSalary(), _perms, _plan, _audit, New FakeArrearsSession())
        _runs.Facts(New Date(2026, 11, 1)) = New List(Of RunEmployeeFacts) From {ArrearsTestData.Facts(10000D, 101), ArrearsTestData.Facts(10000D, 102)}
        AddClaim(11, 101, "E101", 2000D)
        AddClaim(12, 102, "E102", 1000D)
    End Sub

    Private Sub AddClaim(id As Integer, employee As Integer, code As String, gross As Decimal, Optional status As ArrearsClaimStatus = ArrearsClaimStatus.Open)
        Dim row As New ArrearsClaimRow(id, employee, code, "Name " & code, New Date(2026, 5, 1), New Date(2026, 5, 1), "Revision", status, gross, 0D, 0D, 0D, New Date(2026, 11, 1))
        _repo.Rows.Add(row)
        Dim heads As New List(Of ArrearsHeadAmount) From {New ArrearsHeadAmount(1, gross)}
        _repo.Details(id) = New ArrearsClaimDetail(row, New List(Of ArrearsMonthRecord) From {
            New ArrearsMonthRecord(New Date(2026, 5, 1), 0D, gross, gross, gross, gross, gross, gross * 0.12D, 0D, 0D, 0D, 0D, 0D, 0D, heads, 0D, 0D)})
    End Sub

    <Fact>
    Public Sub Create_MakesTheRun_MarksClaimsPaid_ThenSavesLines()
        Dim r As OperationResult(Of ArrearsRunResult) = _service.Create(New Date(2026, 11, 14))
        Assert.True(r.Succeeded)
        Assert.Equal(2, r.Value.RunNo)
        Assert.Equal(2, r.Value.EmployeeCount)
        Assert.Equal(2, r.Value.ClaimCount)
        Assert.Equal(3000D, r.Value.TotalGross)
        Assert.Equal(New Date(2026, 11, 1), _runs.Created.Single().Item1.PeriodFrom)
        Assert.Equal(2, _runs.Created.Single().Item2)
        Assert.Equal(901, _repo.PaidBy.Single().Item1)
        Assert.Equal(2, _repo.PaidBy.Single().Item2.Count)
        Assert.Equal(2, _runs.Saved.Single().Item2.Count)
        Assert.Empty(_repo.Released)
        Assert.Equal("Run 2, 2 employees, 2 claims.", _audit.Entries.Single().Details)
    End Sub

    <Fact>
    Public Sub Preview_StoresNothing()
        Dim r As OperationResult(Of ArrearsRunPlan) = _service.Preview(New Date(2026, 11, 1))
        Assert.True(r.Succeeded)
        Assert.Equal(2, r.Value.Lines.Count)
        Assert.Empty(_runs.Created)
        Assert.Empty(_repo.PaidBy)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub PaidAndCancelledClaims_AreNotPaidAgain()
        _repo.Rows.Clear()
        AddClaim(11, 101, "E101", 2000D, ArrearsClaimStatus.Paid)
        AddClaim(12, 102, "E102", 1000D, ArrearsClaimStatus.Cancelled)
        Dim r As OperationResult(Of ArrearsRunResult) = _service.Create(New Date(2026, 11, 1))
        Assert.False(r.Succeeded)
        Assert.Empty(_runs.Created)
    End Sub

    <Fact>
    Public Sub ClaimThatCannotBePaid_StaysOutAndIsNamed()
        _repo.Rows.Clear()
        AddClaim(11, 101, "E101", 2000D)
        AddClaim(12, 777, "E777", 1000D)
        Dim r As OperationResult(Of ArrearsRunResult) = _service.Create(New Date(2026, 11, 1))
        Assert.True(r.Succeeded)
        Assert.Equal(1, r.Value.ClaimCount)
        Assert.Contains("E777", r.Value.Problems.Single(), StringComparison.Ordinal)
        Assert.Equal(11, _repo.PaidBy.Single().Item2.Single())
    End Sub

    <Fact>
    Public Sub NothingPayable_MakesNoRun()
        _runs.Facts.Clear()
        Dim r As OperationResult(Of ArrearsRunResult) = _service.Create(New Date(2026, 11, 1))
        Assert.False(r.Succeeded)
        Assert.Contains("E101", r.Message, StringComparison.Ordinal)
        Assert.Empty(_runs.Created)
    End Sub

    <Fact>
    Public Sub ClaimTakenByAnotherRun_FailsBeforeAnyLineIsSaved()
        _repo.MarkPaidFail = "A claim is not open any more. Load the list again."
        Dim r As OperationResult(Of ArrearsRunResult) = _service.Create(New Date(2026, 11, 1))
        Assert.False(r.Succeeded)
        Assert.Contains("not open any more", r.Message, StringComparison.Ordinal)
        Assert.Empty(_runs.Saved)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub LinesCannotBeSaved_ClaimsGoBackToOpen()
        _runs.ReplaceFail = "An attendance month in the period is not closed."
        Dim r As OperationResult(Of ArrearsRunResult) = _service.Create(New Date(2026, 11, 1))
        Assert.False(r.Succeeded)
        Assert.Contains("attendance month", r.Message, StringComparison.Ordinal)
        Assert.Equal(901, _repo.Released.Single())
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub NeedsCreate_AndThePlan()
        _perms.Allowed.Remove(PermissionAction.Create)
        Assert.False(_service.Create(New Date(2026, 11, 1)).Succeeded)
        Assert.False(_service.Preview(New Date(2026, 11, 1)).Succeeded)
        _perms.Allowed.Add(PermissionAction.Create)
        _plan.PayrollEnabled = False
        Assert.False(_service.Create(New Date(2026, 11, 1)).Succeeded)
        Assert.Empty(_runs.Created)
    End Sub
End Class
