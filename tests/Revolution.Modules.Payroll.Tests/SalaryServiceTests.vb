Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class SalaryServiceTests
    Private ReadOnly _repo As New FakeSalaryRepository()
    Private ReadOnly _perms As New FakePayPermissions()
    Private ReadOnly _plan As New FakePayEntitlements()
    Private ReadOnly _audit As New FakePayAudit()
    Private ReadOnly _service As SalaryService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Create)
        _perms.Allowed.Add(PermissionAction.Edit)
        _service = New SalaryService(_repo, _perms, _plan, _audit, New FakePaySession())
    End Sub

    Private Shared Function Lines(ParamArray pairs() As Decimal) As IReadOnlyList(Of SalaryLineWrite)
        Dim list As New List(Of SalaryLineWrite)()
        For i As Integer = 0 To pairs.Length - 1 Step 2
            list.Add(New SalaryLineWrite(CInt(pairs(i)), pairs(i + 1)))
        Next
        Return list
    End Function

    <Fact>
    Public Sub GetPolicy_Allowed_ReturnsPolicy()
        Dim r = _service.GetPolicy()
        Assert.True(r.Succeeded)
        Assert.Equal(26D, r.Value.DailyWageDivisor)
    End Sub

    <Fact>
    Public Sub GetPolicy_WithoutView_IsRefused()
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.GetPolicy().Succeeded)
    End Sub

    <Fact>
    Public Sub PlanWithoutPayroll_RefusesEverything()
        _plan.PayrollEnabled = False
        Assert.False(_service.GetPolicy().Succeeded)
        Assert.False(_service.ListComponents().Succeeded)
        Assert.False(_service.SaveSalary(101, New Date(2026, 11, 1), Nothing, Lines(1D, 10000D)).Succeeded)
        Assert.Empty(_repo.SavedSalaries)
    End Sub

    <Fact>
    Public Sub SavePolicy_Valid_SavesWithUserAndAudits()
        Dim r = _service.SavePolicy(New PolicySettings(26, 2D, True, False, 26D, 25D))
        Assert.True(r.Succeeded)
        Assert.Single(_repo.SavedPolicies)
        Assert.Equal(7, _repo.LastUserId)
        Assert.Equal("SavePolicy", _audit.Entries(0).Action)
    End Sub

    <Fact>
    Public Sub SavePolicy_WithoutEdit_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.SavePolicy(New PolicySettings(1, 2D, False, False, 26D, 25D)).Succeeded)
        Assert.Empty(_repo.SavedPolicies)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub SavePolicy_BadCycle_IsRefused()
        Assert.False(_service.SavePolicy(New PolicySettings(15, 2D, False, False, 26D, 25D)).Succeeded)
        Assert.Empty(_repo.SavedPolicies)
    End Sub

    <Fact>
    Public Sub SaveComponent_New_NeedsCreateAndTrimsText()
        Dim r = _service.SaveComponent(New SalaryComponentItem(Nothing, "  CONV ", " Conveyance ", False, False, False, 50, True))
        Assert.True(r.Succeeded)
        Assert.Equal(99, r.Value)
        Assert.Equal("CONV", _repo.SavedComponents(0).Code)
        Assert.Equal("Conveyance", _repo.SavedComponents(0).Name)
        Assert.Equal("AddComponent", _audit.Entries(0).Action)
    End Sub

    <Fact>
    Public Sub SaveComponent_New_WithoutCreate_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Create)
        Assert.False(_service.SaveComponent(New SalaryComponentItem(Nothing, "CONV", "Conveyance", False, False, False, 50, True)).Succeeded)
    End Sub

    <Fact>
    Public Sub SaveComponent_Change_NeedsEdit()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.SaveComponent(New SalaryComponentItem(3, "HRA", "House rent", False, False, False, 30, True)).Succeeded)
    End Sub

    <Fact>
    Public Sub SaveComponent_BasicAndDa_IsRefused()
        Dim r = _service.SaveComponent(New SalaryComponentItem(Nothing, "X", "X", True, True, False, 50, True))
        Assert.False(r.Succeeded)
        Assert.Empty(_repo.SavedComponents)
    End Sub

    <Fact>
    Public Sub SaveComponent_DatabaseRule_BecomesFailedResult()
        _repo.FailWith = "A component with this code already exists."
        Dim r = _service.SaveComponent(New SalaryComponentItem(Nothing, "HRA", "Again", False, False, False, 60, True))
        Assert.False(r.Succeeded)
        Assert.Equal("A component with this code already exists.", r.Message)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub SaveSalary_Valid_SavesAndAuditsWithoutAmount()
        Dim r = _service.SaveSalary(101, New Date(2026, 11, 1), "  Annual revision ", Lines(1D, 15000D, 2D, 3000D, 3D, 2000D))
        Assert.True(r.Succeeded)
        Assert.Equal(500, r.Value)
        Assert.Equal("101:2026-11-01:Annual revision:3", _repo.SavedSalaries(0))
        Assert.Equal(7, _repo.LastUserId)
        Assert.Equal("SaveSalary", _audit.Entries(0).Action)
        Assert.Equal("101", _audit.Entries(0).EntityId)
        Assert.DoesNotContain("15000", _audit.Entries(0).Details)
    End Sub

    <Fact>
    Public Sub SaveSalary_WithoutCreate_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Create)
        Assert.False(_service.SaveSalary(101, New Date(2026, 11, 1), Nothing, Lines(1D, 15000D)).Succeeded)
        Assert.Empty(_repo.SavedSalaries)
    End Sub

    <Fact>
    Public Sub SaveSalary_DateAlreadyHasSalary_ShowsDatabaseMessage()
        _repo.FailWith = "A salary for this date already exists."
        Dim r = _service.SaveSalary(101, New Date(2026, 11, 1), Nothing, Lines(1D, 15000D))
        Assert.False(r.Succeeded)
        Assert.Equal("A salary for this date already exists.", r.Message)
    End Sub

    <Fact>
    Public Sub SaveSalary_AuditFailure_IsReported()
        _audit.Fail = True
        Assert.False(_service.SaveSalary(101, New Date(2026, 11, 1), Nothing, Lines(1D, 15000D)).Succeeded)
    End Sub

    <Fact>
    Public Sub GetSalaryAt_NoSalary_ReturnsNothingValue()
        Dim r = _service.GetSalaryAt(101, New Date(2026, 11, 1))
        Assert.True(r.Succeeded)
        Assert.Null(r.Value)
    End Sub

    <Fact>
    Public Sub GetSalaryAt_ReturnsSalaryInForce()
        _repo.SalaryInForce = New EmployeeSalaryView(5, New Date(2026, 4, 1), New List(Of SalaryLineView) From {
            New SalaryLineView(1, "BASIC", "Basic", True, False, True, 15000D),
            New SalaryLineView(3, "HRA", "House rent", False, False, False, 2000D)})
        Dim r = _service.GetSalaryAt(101, New Date(2026, 11, 1))
        Assert.Equal(17000D, r.Value.MonthlyTotal)
    End Sub

    <Fact>
    Public Sub GetSalaryAt_NoEmployee_IsRefused()
        Assert.False(_service.GetSalaryAt(0, New Date(2026, 11, 1)).Succeeded)
    End Sub
End Class
