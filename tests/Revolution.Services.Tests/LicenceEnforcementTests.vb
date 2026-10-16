Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class LicenceEnforcementTests
    Private ReadOnly _session As New SessionContext()
    Private ReadOnly _repo As New FakePermissionRepository()
    Private ReadOnly _licence As New FakeLicenceService()
    Private ReadOnly _permissions As PermissionService
    Private ReadOnly _entitlements As EntitlementService

    Public Sub New()
        For Each action In {PermissionAction.View, PermissionAction.Create, PermissionAction.Export, PermissionAction.Reveal}
            _repo.Grant(UserRole.Owner, ModuleKey.Payroll, action)
        Next
        _permissions = New PermissionService(_session, _repo, _licence)
        _entitlements = New EntitlementService(_session, _licence)
        _session.SignIn(New UserSession(20, "owner1", UserRole.Owner, 7, PlanTier.Platinum))
    End Sub

    Private Shared Function Licensed(state As LicenceState, plan As PlanTier, ParamArray modules() As ModuleKey) As LicenceStatus
        Dim grant As New LicenceGrant(Guid.NewGuid(), "k1", "developer", New DateOnly(2026, 10, 1), Guid.NewGuid(), "associate1",
                                      New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30), plan, modules, 15, 40)
        Return LicenceStatus.ForLicence(grant, state, 100)
    End Function

    <Fact>
    Public Sub NotLimited_ChangesNothing()
        Assert.True(_permissions.IsAllowed(ModuleKey.Payroll, PermissionAction.Create))
        Assert.Equal(PlanTier.Platinum, _entitlements.CurrentPlan)
        Assert.True(_entitlements.IsEnabled(PlanFeature.Tds))
    End Sub

    <Fact>
    Public Sub ModuleNotInTheLicence_IsDenied_EvenWhenTheRoleHasIt()
        _licence.CurrentStatus = Licensed(LicenceState.Valid, PlanTier.Gold, ModuleKey.Employees)
        Assert.False(_permissions.IsAllowed(ModuleKey.Payroll, PermissionAction.View))
        _licence.CurrentStatus = Licensed(LicenceState.Valid, PlanTier.Gold, ModuleKey.Employees, ModuleKey.Payroll)
        Assert.True(_permissions.IsAllowed(ModuleKey.Payroll, PermissionAction.Create))
    End Sub

    <Fact>
    Public Sub ExpiredLicence_AllowsOnlyViewAndReveal()
        _licence.CurrentStatus = Licensed(LicenceState.Expired, PlanTier.Gold, ModuleKey.Payroll)
        Assert.True(_permissions.IsAllowed(ModuleKey.Payroll, PermissionAction.View))
        Assert.True(_permissions.IsAllowed(ModuleKey.Payroll, PermissionAction.Reveal))
        Assert.False(_permissions.IsAllowed(ModuleKey.Payroll, PermissionAction.Create))
        Assert.False(_permissions.IsAllowed(ModuleKey.Payroll, PermissionAction.Export))
    End Sub

    <Fact>
    Public Sub GraceAndExpiringLicences_AllowEverythingTheRoleHas()
        _licence.CurrentStatus = Licensed(LicenceState.Grace, PlanTier.Gold, ModuleKey.Payroll)
        Assert.True(_permissions.IsAllowed(ModuleKey.Payroll, PermissionAction.Export))
        _licence.CurrentStatus = Licensed(LicenceState.Expiring, PlanTier.Gold, ModuleKey.Payroll)
        Assert.True(_permissions.IsAllowed(ModuleKey.Payroll, PermissionAction.Create))
    End Sub

    <Fact>
    Public Sub Trial_AllowsEverythingWhileDaysRemain_ThenOnlyViewing()
        _licence.CurrentStatus = LicenceStatus.ForTrial(5, LicenceProblem.None, String.Empty)
        Assert.True(_permissions.IsAllowed(ModuleKey.Payroll, PermissionAction.Create))
        _licence.CurrentStatus = LicenceStatus.ForTrial(-1, LicenceProblem.None, String.Empty)
        Assert.True(_permissions.IsAllowed(ModuleKey.Payroll, PermissionAction.View))
        Assert.False(_permissions.IsAllowed(ModuleKey.Payroll, PermissionAction.Create))
    End Sub

    <Fact>
    Public Sub Plan_IsTheLowerOfTheCompanyPlanAndTheLicencePlan()
        _licence.CurrentStatus = Licensed(LicenceState.Valid, PlanTier.Silver, ModuleKey.Payroll)
        Assert.Equal(PlanTier.Silver, _entitlements.CurrentPlan)
        Assert.True(_entitlements.IsEnabled(PlanFeature.Loans))
        Assert.False(_entitlements.IsEnabled(PlanFeature.Tds))
        _licence.CurrentStatus = LicenceStatus.ForTrial(10, LicenceProblem.None, String.Empty)
        Assert.Equal(PlanTier.Copper, _entitlements.CurrentPlan)
        Assert.False(_entitlements.IsEnabled(PlanFeature.Loans))
    End Sub

    <Fact>
    Public Sub LicencePlan_NeverRaisesAPlanAboveTheCompanys()
        _session.SignIn(New UserSession(20, "owner1", UserRole.Owner, 7, PlanTier.Copper))
        _licence.CurrentStatus = Licensed(LicenceState.Valid, PlanTier.Platinum, ModuleKey.Payroll)
        Assert.Equal(PlanTier.Copper, _entitlements.CurrentPlan)
        Assert.False(_entitlements.IsEnabled(PlanFeature.Loans))
    End Sub
End Class
