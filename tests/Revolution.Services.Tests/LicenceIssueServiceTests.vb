Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public NotInheritable Class LicenceIssueServiceTests
    Implements IDisposable

    Private Const KeyPassword As String = "correct horse battery"

    Private ReadOnly _folder As String = IO.Path.Combine(IO.Path.GetTempPath(), "rv-issue-" & Guid.NewGuid().ToString("N"))
    Private ReadOnly _session As New SessionContext()
    Private ReadOnly _permissionRepo As New FakePermissionRepository()
    Private ReadOnly _repo As New FakeLicenceRepository()
    Private ReadOnly _trusted As New Dictionary(Of String, String)()
    Private ReadOnly _audit As New RecordingAuditService()
    Private ReadOnly _service As LicenceIssueService

    Public Sub New()
        IO.Directory.CreateDirectory(_folder)
        _permissionRepo.Grant(UserRole.Developer, ModuleKey.Licence, PermissionAction.View)
        _permissionRepo.Grant(UserRole.Developer, ModuleKey.Licence, PermissionAction.Create)
        Dim permissions As New PermissionService(_session, _permissionRepo)
        Dim admin As New LicenceAdminService(_repo, permissions, _trusted, _audit)
        _service = New LicenceIssueService(admin, permissions, _session, _audit)
        _session.SignIn(New UserSession(1, "dev", UserRole.Developer, 0, PlanTier.Platinum))
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        If IO.Directory.Exists(_folder) Then
            IO.Directory.Delete(_folder, True)
        End If
        GC.SuppressFinalize(Me)
    End Sub

    ''' <summary>Makes a signing key file and, unless told not to, trusts its public key the way a build with LicenceKeys.Trusted would.</summary>
    Private Function MakeKey(Optional trust As Boolean = True) As String
        Dim file = IO.Path.Combine(_folder, "key.key")
        Dim made = _service.CreateSigningKey("test-key", file, KeyPassword)
        Assert.True(made.Succeeded, made.Message)
        If trust Then
            _trusted(made.Value.KeyId) = made.Value.PublicKeyText
        End If
        Return file
    End Function

    Private Shared Function Request(keyFile As String, Optional password As String = KeyPassword, Optional users As Integer = 7,
                                    Optional companies As Integer? = Nothing, Optional validTo As DateOnly? = Nothing,
                                    Optional associate As String = "associate1") As LicenceIssueRequest
        Return New LicenceIssueRequest(associate, PlanTier.Gold, users, companies, New DateOnly(2026, 10, 1), validTo, "first licence", keyFile, password)
    End Function

    <Fact>
    Public Sub CreateSigningKey_SavesAProtectedFile_AndGivesThePublicLine()
        Dim file = IO.Path.Combine(_folder, "new.key")
        Dim made = _service.CreateSigningKey("my-key", file, KeyPassword)
        Assert.True(made.Succeeded)
        Assert.True(IO.File.Exists(file))
        Assert.Contains("my-key", made.Value.TrustedLine)
        Assert.Contains(made.Value.PublicKeyText, made.Value.TrustedLine)
        Assert.DoesNotContain(made.Value.PublicKeyText, IO.File.ReadAllText(file))
        Dim again = _service.CreateSigningKey("my-key", file, KeyPassword)
        Assert.False(again.Succeeded)
        Assert.Contains("already exists", again.Message)
    End Sub

    <Fact>
    Public Sub CreateSigningKey_RefusesAShortPassword_ABadKeyId_AndAnUnauthorisedUser()
        Dim file = IO.Path.Combine(_folder, "short.key")
        Assert.False(_service.CreateSigningKey("my-key", file, "short").Succeeded)
        Assert.False(_service.CreateSigningKey("bad key!", file, KeyPassword).Succeeded)
        Assert.False(IO.File.Exists(file))
        _session.SignIn(New UserSession(10, "associate1", UserRole.Associate, 7, PlanTier.Gold))
        Assert.False(_service.CreateSigningKey("my-key", file, KeyPassword).Succeeded)
    End Sub

    <Fact>
    Public Sub Issue_SignsImportsAndSavesABackupAndALogLine()
        Dim keyFile = MakeKey()
        Dim result = _service.Issue(Request(keyFile))
        Assert.True(result.Succeeded, result.Message)
        Dim grant = Assert.Single(_repo.Imported)
        Assert.Equal("associate1", grant.Associate)
        Assert.Equal(PlanTier.Gold, grant.PlanCap)
        Assert.Equal(7, grant.MaxActiveUsers)
        Assert.Equal(35, grant.MaxCompanies)
        Assert.Equal(New DateOnly(2027, 9, 30), grant.ValidTo)
        Assert.Equal("dev", grant.IssuedBy)
        Assert.True(IO.File.Exists(result.Value.BackupFile))
        Assert.Equal(String.Empty, result.Value.Warning)
        Dim log = IO.File.ReadAllText(IO.Path.Combine(_folder, "issued-licences.log"))
        Assert.Contains("associate1", log)
        Assert.Contains(grant.LicenceId.ToString(), log)
        Assert.Contains(_audit.Entries, Function(entry) entry.Action = "LicenceIssued")
    End Sub

    <Fact>
    Public Sub Issue_UsesTheGivenCompanyCapAndLastDay()
        Dim result = _service.Issue(Request(MakeKey(), companies:=12, validTo:=New DateOnly(2027, 3, 31)))
        Assert.True(result.Succeeded, result.Message)
        Assert.Equal(12, _repo.Imported(0).MaxCompanies)
        Assert.Equal(New DateOnly(2027, 3, 31), _repo.Imported(0).ValidTo)
    End Sub

    <Fact>
    Public Sub Issue_WithAWrongPassword_ImportsNothing()
        Dim keyFile = MakeKey()
        Dim result = _service.Issue(Request(keyFile, password:="not the password"))
        Assert.False(result.Succeeded)
        Assert.Contains("wrong password", result.Message)
        Assert.Empty(_repo.Imported)
    End Sub

    <Fact>
    Public Sub Issue_WithAKeyThisBuildDoesNotTrust_IsRefusedByTheImport()
        Dim result = _service.Issue(Request(MakeKey(trust:=False)))
        Assert.False(result.Succeeded)
        Assert.Empty(_repo.Imported)
        Assert.False(IO.Directory.Exists(IO.Path.Combine(_folder, "licences")))
    End Sub

    <Fact>
    Public Sub Issue_ByAnAssociate_IsRefused()
        Dim keyFile = MakeKey()
        _session.SignIn(New UserSession(10, "associate1", UserRole.Associate, 7, PlanTier.Gold))
        Assert.False(_service.Issue(Request(keyFile)).Succeeded)
        Assert.Empty(_repo.Imported)
    End Sub

    <Fact>
    Public Sub Issue_RefusesBadInput()
        Dim keyFile = MakeKey()
        Assert.False(_service.Issue(Request(keyFile, users:=0)).Succeeded)
        Assert.False(_service.Issue(Request(keyFile, companies:=501)).Succeeded)
        Assert.False(_service.Issue(Request(keyFile, validTo:=New DateOnly(2026, 9, 30))).Succeeded)
        Assert.False(_service.Issue(Request(keyFile, associate:="two words")).Succeeded)
        Assert.False(_service.Issue(Request(keyFile, associate:="")).Succeeded)
        Assert.False(_service.Issue(Request(IO.Path.Combine(_folder, "missing.key"))).Succeeded)
        Assert.Empty(_repo.Imported)
    End Sub
End Class
