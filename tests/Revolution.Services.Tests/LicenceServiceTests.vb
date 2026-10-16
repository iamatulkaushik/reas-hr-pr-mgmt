Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class LicenceServiceTests
    Private ReadOnly _session As New SessionContext()
    Private ReadOnly _repo As New FakeLicenceRepository()
    Private ReadOnly _issuer As New LicenceTestIssuer("k1")
    Private ReadOnly _svc As LicenceService

    Public Sub New()
        _svc = New LicenceService(_session, _repo, _issuer.TrustedKeys())
        _session.SignIn(New UserSession(10, "associate1", UserRole.Associate, 7, PlanTier.Gold))
    End Sub

    Private Sub Store(validFrom As DateOnly, validTo As DateOnly, Optional associate As String = "associate1", Optional storedFor As String = "associate1")
        Dim grant = _issuer.Grant(_repo.Installation.Id, associate, validFrom, validTo, PlanTier.Gold, ModuleKey.Employees, ModuleKey.Payroll)
        _repo.Stored = New StoredLicence(storedFor, _issuer.Issue(grant))
    End Sub

    <Fact>
    Public Sub Developer_AndSignedOutSession_AreNeverLimited()
        _session.SignIn(New UserSession(1, "dev", UserRole.Developer, 0, PlanTier.Platinum))
        Assert.Equal(LicenceKind.NotLimited, _svc.Status.Kind)
        _session.SignOut()
        Assert.Equal(LicenceKind.NotLimited, _svc.Status.Kind)
        Assert.False(_svc.Status.IsReadOnly)
    End Sub

    <Fact>
    Public Sub NoLicence_IsTheTrial_CountedFromTheInstallationDate()
        Dim status = _svc.Status
        Assert.Equal(LicenceKind.Trial, status.Kind)
        Assert.Equal(19, status.DaysLeft)
        Assert.Equal(PlanTier.Copper, status.PlanCap.Value)
        Assert.Equal(LicenceProblem.None, status.Problem)
        Assert.False(status.IsReadOnly)
        Assert.Contains("19", status.Banner)
    End Sub

    <Fact>
    Public Sub Trial_AfterTwentyEightDays_IsReadOnly()
        _repo.Today = New DateOnly(2026, 10, 30)
        Assert.Equal(-1, _svc.Status.DaysLeft)
        Assert.True(_svc.Status.IsReadOnly)
        _svc.Refresh()
        _repo.Today = New DateOnly(2026, 10, 29)
        Assert.Equal(0, _svc.Status.DaysLeft)
        Assert.False(_svc.Status.IsReadOnly)
    End Sub

    <Fact>
    Public Sub GenuineLicence_Governs_AndHasNoBanner()
        Store(New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30))
        Dim status = _svc.Status
        Assert.Equal(LicenceKind.Licensed, status.Kind)
        Assert.Equal(LicenceState.Valid, status.State)
        Assert.Equal(PlanTier.Gold, status.PlanCap.Value)
        Assert.True(status.AllowsModule(ModuleKey.Payroll))
        Assert.False(status.AllowsModule(ModuleKey.Statutory))
        Assert.Equal(String.Empty, status.Banner)
    End Sub

    <Fact>
    Public Sub Licence_NearTheEnd_WarnsThenGraceThenReadOnly()
        Store(New DateOnly(2026, 10, 1), New DateOnly(2026, 10, 20))
        Assert.Equal(LicenceState.Expiring, _svc.Status.State)
        Assert.Contains("10 day", _svc.Status.Banner)
        _repo.Today = New DateOnly(2026, 10, 23)
        _svc.Refresh()
        Assert.Equal(LicenceState.Grace, _svc.Status.State)
        Assert.Contains("3 day", _svc.Status.Banner)
        Assert.False(_svc.Status.IsReadOnly)
        _repo.Today = New DateOnly(2026, 11, 5)
        _svc.Refresh()
        Assert.Equal(LicenceState.Expired, _svc.Status.State)
        Assert.True(_svc.Status.IsReadOnly)
    End Sub

    <Fact>
    Public Sub LicenceForAnotherAssociate_IsRefused_AndTheTrialApplies()
        Store(New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30), associate:="someone_else")
        Assert.Equal(LicenceKind.Trial, _svc.Status.Kind)
        Assert.Equal(LicenceProblem.WrongAssociate, _svc.Status.Problem)
    End Sub

    <Fact>
    Public Sub UntrustedKey_IsRefused_AndTheTrialApplies()
        Store(New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30))
        Dim svc As New LicenceService(_session, _repo, New Dictionary(Of String, String)())
        Assert.Equal(LicenceKind.Trial, svc.Status.Kind)
        Assert.Equal(LicenceProblem.UnknownKey, svc.Status.Problem)
    End Sub

    <Fact>
    Public Sub LicenceForAnotherInstallation_IsRefused()
        Dim grant = _issuer.Grant(Guid.NewGuid(), "associate1", New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30), PlanTier.Gold, ModuleKey.Payroll)
        _repo.Stored = New StoredLicence("associate1", _issuer.Issue(grant))
        Assert.Equal(LicenceProblem.WrongInstallation, _svc.Status.Problem)
        Assert.Equal(LicenceKind.Trial, _svc.Status.Kind)
    End Sub

    <Fact>
    Public Sub LicenceThatHasNotStarted_IsTheTrialWithAMessage()
        Store(New DateOnly(2026, 11, 1), New DateOnly(2027, 10, 31))
        Assert.Equal(LicenceKind.Trial, _svc.Status.Kind)
        Assert.Equal(LicenceProblem.NotYetValid, _svc.Status.Problem)
        Assert.Contains("01-11-2026", _svc.Status.ProblemMessage)
    End Sub

    <Fact>
    Public Sub Status_IsRemembered_UntilRefreshOrTheCompanyChanges()
        Store(New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30))
        Assert.Equal(LicenceState.Valid, _svc.Status.State)
        _repo.Today = New DateOnly(2027, 12, 1)
        Assert.Equal(LicenceState.Valid, _svc.Status.State)
        _session.SignIn(New UserSession(10, "associate1", UserRole.Associate, 8, PlanTier.Gold))
        Assert.Equal(LicenceState.Expired, _svc.Status.State)
    End Sub
End Class
