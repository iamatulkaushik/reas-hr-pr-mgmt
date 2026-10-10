Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class PermissionServiceTests
    Private ReadOnly _session As New SessionContext()
    Private ReadOnly _repo As New FakePermissionRepository()
    Private ReadOnly _svc As PermissionService

    Public Sub New()
        _repo.Grant(UserRole.Owner, ModuleKey.Payroll, PermissionAction.Approve)
        _svc = New PermissionService(_session, _repo)
    End Sub

    <Fact>
    Public Sub NotSignedIn_IsDenied()
        Assert.False(_svc.IsAllowed(ModuleKey.Payroll, PermissionAction.Approve))
    End Sub

    <Fact>
    Public Sub Owner_GrantedAction_IsAllowed_OthersAreNot()
        _session.SignIn(TestData.Session(UserRole.Owner, PlanTier.Gold))
        Assert.True(_svc.IsAllowed(ModuleKey.Payroll, PermissionAction.Approve))
        Assert.False(_svc.IsAllowed(ModuleKey.Payroll, PermissionAction.Delete))
        Assert.False(_svc.IsAllowed(ModuleKey.Employees, PermissionAction.Approve))
    End Sub

    <Fact>
    Public Sub Operator_WithoutGrant_IsDenied()
        _session.SignIn(TestData.Session(UserRole.Operator, PlanTier.Gold))
        Assert.False(_svc.IsAllowed(ModuleKey.Payroll, PermissionAction.Approve))
    End Sub

    <Fact>
    Public Sub MustChangePassword_DeniesEverything()
        _session.SignIn(TestData.Session(UserRole.Owner, PlanTier.Gold, mustChange:=True))
        Assert.False(_svc.IsAllowed(ModuleKey.Payroll, PermissionAction.Approve))
    End Sub

    <Fact>
    Public Sub Grants_AreLoadedOncePerRole_UntilInvalidated()
        _session.SignIn(TestData.Session(UserRole.Owner, PlanTier.Gold))
        _svc.IsAllowed(ModuleKey.Payroll, PermissionAction.Approve)
        _svc.IsAllowed(ModuleKey.Payroll, PermissionAction.View)
        Assert.Equal(1, _repo.LoadCount)
        _svc.InvalidateCache()
        _svc.IsAllowed(ModuleKey.Payroll, PermissionAction.View)
        Assert.Equal(2, _repo.LoadCount)
    End Sub
End Class
