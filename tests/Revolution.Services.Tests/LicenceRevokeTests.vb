Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

''' <summary>Revoke and reinstate (ADR 0018 amendment, migration 0025): the admin service, the overview, and what a revoked licence does to a session.</summary>
Public Class LicenceRevokeTests
    Private ReadOnly _session As New SessionContext()
    Private ReadOnly _permissionRepo As New FakePermissionRepository()
    Private ReadOnly _repo As New FakeLicenceRepository()
    Private ReadOnly _issuer As New LicenceTestIssuer("k1")
    Private ReadOnly _audit As New RecordingAuditService()
    Private ReadOnly _admin As LicenceAdminService
    Private ReadOnly _licence As LicenceService

    Public Sub New()
        _permissionRepo.Grant(UserRole.Developer, ModuleKey.Licence, PermissionAction.View)
        _permissionRepo.Grant(UserRole.Developer, ModuleKey.Licence, PermissionAction.Edit)
        _admin = New LicenceAdminService(_repo, New PermissionService(_session, _permissionRepo), _issuer.TrustedKeys(), _audit)
        _licence = New LicenceService(_session, _repo, _issuer.TrustedKeys())
        _session.SignIn(New UserSession(1, "dev", UserRole.Developer, 0, PlanTier.Platinum))
    End Sub

    Private Function SignedLicence(validFrom As DateOnly, validTo As DateOnly) As String
        Return _issuer.Issue(_issuer.Grant(_repo.Installation.Id, "associate1", validFrom, validTo, PlanTier.Gold, ModuleKey.Employees, ModuleKey.Payroll))
    End Function

    Private Sub StoreRevokedForAssociate(validFrom As DateOnly, validTo As DateOnly)
        _repo.Stored = New StoredLicence("associate1", SignedLicence(validFrom, validTo), New DateOnly(2026, 10, 9), "Unpaid invoice")
        _session.SignIn(New UserSession(10, "associate1", UserRole.Associate, 7, PlanTier.Gold))
    End Sub

    ' ---- The admin service ----

    <Fact>
    Public Sub Revoke_IsRecordedAndAudited_WithTheReason()
        Dim result = _admin.Revoke(" associate1 ", "  Unpaid invoice  ")
        Assert.True(result.Succeeded)
        Assert.Equal({"associate1|Unpaid invoice"}, _repo.Revoked)
        Assert.Single(_audit.Entries)
        Assert.Equal("LicenceRevoked", _audit.Entries(0).Action)
    End Sub

    <Fact>
    Public Sub Revoke_NeedsAnAssociateAndAReason()
        Assert.False(_admin.Revoke("associate1", "   ").Succeeded)
        Assert.False(_admin.Revoke("  ", "Unpaid invoice").Succeeded)
        Assert.False(_admin.Revoke("associate1", New String("x"c, 201)).Succeeded)
        Assert.Empty(_repo.Revoked)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub Revoke_AndReinstate_AreRefusedWithoutEditRightsOnTheLicenceModule()
        _session.SignIn(New UserSession(10, "associate1", UserRole.Associate, 7, PlanTier.Gold))
        Assert.False(_admin.Revoke("associate1", "Unpaid invoice").Succeeded)
        Assert.False(_admin.Reinstate("associate1").Succeeded)
        Assert.Empty(_repo.Revoked)
        Assert.Empty(_repo.Reinstated)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub Reinstate_IsRecordedAndAudited()
        Dim result = _admin.Reinstate("associate1")
        Assert.True(result.Succeeded)
        Assert.Equal({"associate1"}, _repo.Reinstated)
        Assert.Equal("LicenceReinstated", _audit.Entries.Single().Action)
    End Sub

    <Fact>
    Public Sub Overview_ShowsARevokedLicence_WithTheReason()
        _repo.Current.Add(New LicenceRecord("associate1", SignedLicence(New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30)),
                                            New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30), PlanTier.Gold, 15, 40,
                                            New DateOnly(2026, 10, 9), "Unpaid invoice"))
        Dim listed = _admin.ListCurrent()
        Assert.True(listed.Succeeded)
        Dim overview = listed.Value.Single()
        Assert.Equal(LicenceState.Revoked, overview.State.Value)
        Assert.StartsWith("Revoked (read-only)", overview.StatusText)
        Assert.Contains("Unpaid invoice", overview.StatusText)
    End Sub

    <Fact>
    Public Sub Overview_ShowsNotValid_WhenARevokedLicenceDoesNotVerify()
        _repo.Current.Add(New LicenceRecord("associate1", "garbage", New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30),
                                            PlanTier.Gold, 15, 40, New DateOnly(2026, 10, 9), "Unpaid invoice"))
        Dim overview = _admin.ListCurrent().Value.Single()
        Assert.False(overview.State.HasValue)
        Assert.StartsWith("Not valid", overview.StatusText)
    End Sub

    ' ---- What a revoked licence does to a session ----

    <Fact>
    Public Sub RevokedLicence_IsReadOnlyAtOnce_WithNoGraceAndNoTrial()
        StoreRevokedForAssociate(New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30))
        Dim status = _licence.Status
        Assert.Equal(LicenceKind.Licensed, status.Kind)
        Assert.Equal(LicenceState.Revoked, status.State)
        Assert.True(status.IsReadOnly)
        Assert.Contains("withdrawn", status.Banner)
    End Sub

    <Fact>
    Public Sub RevokedLicence_StaysRevoked_EvenBeforeItsStartDate()
        StoreRevokedForAssociate(New DateOnly(2026, 12, 1), New DateOnly(2027, 11, 30))
        Assert.Equal(LicenceState.Revoked, _licence.Status.State)
    End Sub

    <Fact>
    Public Sub ALicenceThatIsNotRevoked_IsUnaffected()
        _repo.Stored = New StoredLicence("associate1", SignedLicence(New DateOnly(2026, 10, 1), New DateOnly(2027, 9, 30)))
        _session.SignIn(New UserSession(10, "associate1", UserRole.Associate, 7, PlanTier.Gold))
        Assert.Equal(LicenceState.Valid, _licence.Status.State)
        Assert.False(_licence.Status.IsReadOnly)
    End Sub
End Class
