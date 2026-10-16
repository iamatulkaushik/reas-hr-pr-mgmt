Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class LicenceAdminServiceTests
    Private ReadOnly _session As New SessionContext()
    Private ReadOnly _permissionRepo As New FakePermissionRepository()
    Private ReadOnly _repo As New FakeLicenceRepository()
    Private ReadOnly _issuer As New LicenceTestIssuer("k1")
    Private ReadOnly _audit As New RecordingAuditService()
    Private ReadOnly _service As LicenceAdminService

    Public Sub New()
        _permissionRepo.Grant(UserRole.Developer, ModuleKey.Licence, PermissionAction.View)
        _permissionRepo.Grant(UserRole.Developer, ModuleKey.Licence, PermissionAction.Create)
        _service = New LicenceAdminService(_repo, New PermissionService(_session, _permissionRepo), _issuer.TrustedKeys(), _audit)
        _session.SignIn(New UserSession(1, "dev", UserRole.Developer, 0, PlanTier.Platinum))
    End Sub

    Private Function Signed(validFrom As DateOnly, validTo As DateOnly, Optional installationId As Guid = Nothing) As String
        Dim target = If(installationId = Guid.Empty, _repo.Installation.Id, installationId)
        Return _issuer.Issue(_issuer.Grant(target, "associate1", validFrom, validTo, PlanTier.Gold, ModuleKey.Payroll))
    End Function

    <Fact>
    Public Sub Import_StoresAVerifiedLicence_AndAudits()
        Dim result = _service.Import(Signed(New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30)))
        Assert.True(result.Succeeded)
        Assert.Single(_repo.Imported)
        Assert.Equal("associate1", _repo.Imported(0).Associate)
        Assert.Single(_audit.Entries)
        Assert.Equal("LicenceImported", _audit.Entries(0).Action)
    End Sub

    <Fact>
    Public Sub Import_ByAnUnauthorisedUser_IsRefused()
        _session.SignIn(New UserSession(10, "associate1", UserRole.Associate, 7, PlanTier.Gold))
        Dim result = _service.Import(Signed(New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30)))
        Assert.False(result.Succeeded)
        Assert.Empty(_repo.Imported)
    End Sub

    <Fact>
    Public Sub Import_RefusesGarbage_AndAnotherInstallation_AndAnExpiredLicence()
        Assert.False(_service.Import("garbage").Succeeded)
        Dim elsewhere = _service.Import(Signed(New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30), Guid.NewGuid()))
        Assert.False(elsewhere.Succeeded)
        Assert.Contains("different installation", elsewhere.Message)
        Dim expired = _service.Import(Signed(New DateOnly(2025, 10, 1), New DateOnly(2026, 1, 31)))
        Assert.False(expired.Succeeded)
        Assert.Contains("expired", expired.Message)
        Assert.Empty(_repo.Imported)
    End Sub

    <Fact>
    Public Sub Import_AcceptsALicenceThatStartsLater()
        Assert.True(_service.Import(Signed(New DateOnly(2026, 12, 1), New DateOnly(2027, 11, 30))).Succeeded)
    End Sub

    <Fact>
    Public Sub ListCurrent_ShowsEachLicencesStateOrProblem()
        _repo.Current.Add(New LicenceRecord("associate1", Signed(New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30)),
                                            New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30), PlanTier.Gold, 15, 40))
        _repo.Current.Add(New LicenceRecord("associate2", Signed(New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30)),
                                            New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30), PlanTier.Gold, 15, 40))
        _repo.Current.Add(New LicenceRecord("associate3", "garbage", New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30), PlanTier.Copper, 5, 5))
        Dim result = _service.ListCurrent()
        Assert.True(result.Succeeded)
        Dim lines = result.Value
        Assert.Equal("Valid", lines(0).StatusText)
        Assert.Equal("30-09-2027", lines(0).ValidToText)
        Assert.Equal(LicenceProblem.WrongAssociate, lines(1).Problem)
        Assert.StartsWith("Not valid", lines(1).StatusText)
        Assert.Equal(LicenceProblem.Malformed, lines(2).Problem)
    End Sub

    <Fact>
    Public Sub ListCurrent_WithoutPermission_IsRefused()
        _session.SignIn(New UserSession(10, "associate1", UserRole.Associate, 7, PlanTier.Gold))
        Assert.False(_service.ListCurrent().Succeeded)
    End Sub
End Class
