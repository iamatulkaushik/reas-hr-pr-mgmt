Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class EntitlementServiceTests
    Private ReadOnly _session As New SessionContext()
    Private ReadOnly _svc As EntitlementService

    Public Sub New()
        _svc = New EntitlementService(_session)
    End Sub

    <Fact>
    Public Sub NotSignedIn_NothingIsEnabled()
        Assert.False(_svc.IsEnabled(PlanFeature.Employees))
        Assert.Equal(PlanTier.Copper, _svc.CurrentPlan)
    End Sub

    <Fact>
    Public Sub Copper_HasEmployees_NotTds()
        _session.SignIn(TestData.Session(UserRole.Owner, PlanTier.Copper))
        Assert.True(_svc.IsEnabled(PlanFeature.Employees))
        Assert.False(_svc.IsEnabled(PlanFeature.Tds))
    End Sub

    <Fact>
    Public Sub Platinum_HasEverything()
        _session.SignIn(TestData.Session(UserRole.Owner, PlanTier.Platinum))
        Assert.True(_svc.IsEnabled(PlanFeature.MultiCompany))
        Assert.Equal(PlanTier.Platinum, _svc.CurrentPlan)
    End Sub
End Class
