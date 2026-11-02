Imports Revolution.Core
Imports Revolution.Modules.Arrears
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class ArrearsServiceTests
    Private ReadOnly _repo As New FakeArrearsRepository()
    Private ReadOnly _perms As New FakeArrearsPermissions()
    Private ReadOnly _plan As New FakeArrearsEntitlements()
    Private ReadOnly _audit As New FakeArrearsAudit()
    Private ReadOnly _service As ArrearsService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Create)
        _perms.Allowed.Add(PermissionAction.Edit)
        _service = New ArrearsService(_repo, _perms, _plan, _audit, New FakeArrearsSession())
    End Sub

    Private Shared Function TwoMonths() As List(Of ArrearsMonthInput)
        Return New List(Of ArrearsMonthInput) From {ArrearsTestRates.Month(2026, 6, 10000D, 12000D), ArrearsTestRates.Month(2026, 5, 10000D, 12000D)}
    End Function

    <Fact>
    Public Sub PlanWithoutPayroll_RefusesEverything()
        _plan.PayrollEnabled = False
        Assert.False(_service.Create(101, "Revision", TwoMonths()).Succeeded)
        Assert.False(_service.List(Nothing).Succeeded)
        Assert.False(_service.Report(New Date(2026, 5, 1), New Date(2026, 6, 1)).Succeeded)
        Assert.Empty(_repo.Added)
    End Sub

    <Fact>
    Public Sub Create_NeedsCreate()
        _perms.Allowed.Remove(PermissionAction.Create)
        Assert.False(_service.Create(101, "Revision", TwoMonths()).Succeeded)
        Assert.Empty(_repo.Added)
    End Sub

    <Fact>
    Public Sub Create_StoresMonthsOldestFirst_WithCalculatedAmounts_AndAudits()
        Dim r As OperationResult(Of Integer) = _service.Create(101, " Revision of basic ", TwoMonths())
        Assert.True(r.Succeeded)
        Dim claim As NewArrearsClaim = _repo.Added.Single()
        Assert.Equal("Revision of basic", claim.Reason)
        Assert.Equal(7, claim.UserId.Value)
        Assert.Equal(5, claim.Months(0).WageMonth.Month)
        Assert.Equal(2000D, claim.Months(0).GrossDifference)
        Assert.Equal(10000D, claim.Months(0).PaidGross)
        Assert.Equal(12000D, claim.Months(0).DueGross)
        Assert.Equal(240D, claim.Months(0).EmployeeEpf)
        Assert.Equal(15D, claim.Months(0).EsiEmployee)
        Assert.Equal("Arrears.Add", _audit.Entries.Single().Action)
    End Sub

    <Fact>
    Public Sub Create_BadInput_IsRefused()
        Assert.False(_service.Create(0, "Revision", TwoMonths()).Succeeded)
        Assert.False(_service.Create(101, "  ", TwoMonths()).Succeeded)
        Assert.False(_service.Create(101, New String("x"c, 201), TwoMonths()).Succeeded)
        Assert.False(_service.Create(101, "Revision", New List(Of ArrearsMonthInput)()).Succeeded)
        Assert.False(_service.Create(101, "Revision", New List(Of ArrearsMonthInput) From {
            ArrearsTestRates.Month(2026, 5, 1D, 2D), ArrearsTestRates.Month(2026, 5, 1D, 3D)}).Succeeded)
        Assert.Empty(_repo.Added)
    End Sub

    <Fact>
    Public Sub Create_MoreThanSixtyMonths_IsRefused()
        Dim many As New List(Of ArrearsMonthInput)()
        For i As Integer = 0 To ArrearsService.MaxMonths
            many.Add(ArrearsTestRates.Month(2020 + i \ 12, i Mod 12 + 1, 1D, 2D))
        Next
        Assert.False(_service.Create(101, "Revision", many).Succeeded)
    End Sub

    <Fact>
    Public Sub Create_DatabaseRule_BecomesAFailedResult()
        _repo.FailWith = "A month is already under an open arrears claim for this employee."
        Dim r As OperationResult(Of Integer) = _service.Create(101, "Revision", TwoMonths())
        Assert.False(r.Succeeded)
        Assert.Contains("already under an open", r.Message, StringComparison.Ordinal)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub List_NeedsView_AndPassesTheFilter()
        Dim filter As New ArrearsFilter With {.EmployeeId = 101, .IncludeCancelled = True}
        Assert.True(_service.List(filter).Succeeded)
        Assert.Equal(101, _repo.LastFilter.EmployeeId.Value)
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.List(filter).Succeeded)
    End Sub

    <Fact>
    Public Sub List_BackwardsMonths_AreRefused()
        Dim filter As New ArrearsFilter With {.FromMonth = New Date(2026, 6, 1), .ToMonth = New Date(2026, 5, 1)}
        Assert.False(_service.List(filter).Succeeded)
    End Sub

    <Fact>
    Public Sub Cancel_NeedsEditAndAReason_AndAudits()
        Assert.False(_service.Cancel(1, " ").Succeeded)
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.Cancel(1, "Wrong employee").Succeeded)
        _perms.Allowed.Add(PermissionAction.Edit)
        Assert.True(_service.Cancel(1, " Wrong employee ").Succeeded)
        Assert.Equal("Wrong employee", _repo.Cancelled.Single().Item2)
        Assert.Equal("Arrears.Cancel", _audit.Entries.Single().Action)
    End Sub

    <Fact>
    Public Sub Cancel_DatabaseRule_BecomesAFailedResult()
        _repo.FailWith = "The arrears claim is already cancelled."
        Assert.False(_service.Cancel(1, "Wrong employee").Succeeded)
    End Sub

    <Fact>
    Public Sub Report_SnapsToFirstOfMonth_AndRefusesBadRanges()
        Dim r As OperationResult(Of ArrearsReport) = _service.Report(New Date(2026, 5, 17), New Date(2026, 6, 3))
        Assert.True(r.Succeeded)
        Assert.Equal(New Date(2026, 5, 1), _repo.LastReport.Item1)
        Assert.Equal(New Date(2026, 6, 1), _repo.LastReport.Item2)
        Assert.False(_service.Report(New Date(2026, 6, 1), New Date(2026, 5, 1)).Succeeded)
        Assert.False(_service.Report(New Date(2020, 1, 1), New Date(2026, 1, 1)).Succeeded)
    End Sub
End Class
