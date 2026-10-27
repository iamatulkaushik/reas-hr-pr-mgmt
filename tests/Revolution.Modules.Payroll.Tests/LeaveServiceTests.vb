Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class LeaveServiceTests
    Private ReadOnly _repo As New FakeLeaveRepository()
    Private ReadOnly _perms As New FakePayPermissions()
    Private ReadOnly _plan As New FakePayEntitlements()
    Private ReadOnly _audit As New FakePayAudit()
    Private ReadOnly _service As LeaveService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Create)
        _service = New LeaveService(_repo, _perms, _plan, _audit, New FakePaySession())
    End Sub

    Private Shared Function Row(balance As Decimal) As LeaveBalanceRow
        Return New LeaveBalanceRow(1, "EL", "Earned leave", True, 30D, balance, 0D, 0D, 0D, 0D, 0D, balance)
    End Function

    <Fact>
    Public Sub PlanWithoutFullAndFinal_RefusesEverything()
        _plan.Disabled.Add(PlanFeature.FullAndFinal)
        Assert.False(_service.ListTypes().Succeeded)
        Assert.False(_service.Post(101, 1, New Date(2026, 10, 1), LeaveEntryKind.Accrual, 1D, Nothing).Succeeded)
        Assert.Empty(_repo.Posted)
    End Sub

    <Fact>
    Public Sub SaveType_New_NeedsCreate_TrimsUppercasesAndAudits()
        Dim r = _service.SaveType(New LeaveTypeItem(Nothing, " el2 ", " Extra leave ", True, 10D, Nothing, True))
        Assert.True(r.Succeeded)
        Assert.Equal("EL2", _repo.SavedTypes.Single().Code)
        Assert.Equal("Extra leave", _repo.SavedTypes.Single().Name)
        Assert.Equal("LeaveType.Add", _audit.Entries.Single().Action)
    End Sub

    <Fact>
    Public Sub SaveType_Change_NeedsEdit()
        Assert.False(_service.SaveType(New LeaveTypeItem(1, "EL", "Earned leave", True, 30D, 30D, True)).Succeeded)
        _perms.Allowed.Add(PermissionAction.Edit)
        Assert.True(_service.SaveType(New LeaveTypeItem(1, "EL", "Earned leave", True, 30D, 30D, True)).Succeeded)
    End Sub

    <Fact>
    Public Sub SaveType_LimitWithoutEncashing_Refused()
        Assert.False(_service.SaveType(New LeaveTypeItem(Nothing, "CL", "Casual", False, 5D, Nothing, True)).Succeeded)
        Assert.False(_service.SaveType(New LeaveTypeItem(Nothing, "CL", "Casual", True, 0D, Nothing, True)).Succeeded)
    End Sub

    <Fact>
    Public Sub Post_UsesFinancialYearOfTheDateAndAudits()
        Dim r = _service.Post(101, 1, New Date(2027, 2, 10), LeaveEntryKind.Accrual, 1.5D, " work ")
        Assert.True(r.Succeeded)
        Assert.Equal(700L, r.Value)
        Assert.Equal(2026, _repo.LastLeaveYear)
        Assert.Equal(7, _repo.LastUserId)
        Assert.Equal("101:1:Accrual:1.5:work", _repo.Posted.Single())
        Assert.Equal("Leave.Post", _audit.Entries.Single().Action)
    End Sub

    <Fact>
    Public Sub Post_Adjustment_NeedsEdit()
        Assert.False(_service.Post(101, 1, New Date(2026, 10, 1), LeaveEntryKind.Adjustment, -2D, "fix").Succeeded)
        _perms.Allowed.Add(PermissionAction.Edit)
        Assert.True(_service.Post(101, 1, New Date(2026, 10, 1), LeaveEntryKind.Adjustment, -2D, "fix").Succeeded)
    End Sub

    <Fact>
    Public Sub Post_Refusals()
        Assert.False(_service.Post(0, 1, New Date(2026, 10, 1), LeaveEntryKind.Accrual, 1D, Nothing).Succeeded)
        Assert.False(_service.Post(101, 9, New Date(2026, 10, 1), LeaveEntryKind.Accrual, 1D, Nothing).Succeeded)
        Assert.False(_service.Post(101, 1, New Date(2026, 10, 1), LeaveEntryKind.Taken, -1D, Nothing).Succeeded)
        Assert.False(_service.Post(101, 1, New Date(2026, 10, 1), LeaveEntryKind.Taken, 1.234D, Nothing).Succeeded)
        Assert.False(_service.Post(101, 2, New Date(2026, 10, 1), LeaveEntryKind.Encashed, 1D, Nothing).Succeeded)
        Assert.False(_service.Post(101, 1, New Date(2026, 10, 1), LeaveEntryKind.CarryForward, 31D, Nothing).Succeeded)
        Assert.Empty(_repo.Posted)
    End Sub

    <Fact>
    Public Sub Post_DatabaseRuleError_BecomesFailure()
        _repo.FailWith = "The balance would go below zero."
        Dim r = _service.Post(101, 1, New Date(2026, 10, 1), LeaveEntryKind.Taken, 5D, Nothing)
        Assert.False(r.Succeeded)
        Assert.Equal("The balance would go below zero.", r.Message)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub EncashmentFor_CutsToTheTypeLimit()
        _repo.Balances.Add(Row(40D))
        Dim r = _service.EncashmentFor(101, 1, 1000D)
        Assert.True(r.Succeeded)
        Assert.Equal(30D, r.Value.Days)
        Assert.Equal(30000D, r.Value.Amount)
        Assert.True(r.Value.WasCapped)
    End Sub

    <Fact>
    Public Sub EncashmentFor_NoBalanceRow_PaysZero()
        Assert.Equal(0D, _service.EncashmentFor(101, 1, 1000D).Value.Amount)
    End Sub

    <Fact>
    Public Sub EncashmentFor_NotEncashableType_Refused()
        Assert.False(_service.EncashmentFor(101, 2, 1000D).Succeeded)
        Assert.False(_service.EncashmentFor(101, 99, 1000D).Succeeded)
    End Sub

    <Fact>
    Public Sub Lists_NeedAnEmployee()
        Assert.False(_service.ListBalances(0).Succeeded)
        Assert.False(_service.ListEntries(0, Nothing).Succeeded)
        Assert.True(_service.ListBalances(101).Succeeded)
    End Sub
End Class
