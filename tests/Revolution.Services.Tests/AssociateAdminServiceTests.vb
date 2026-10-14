Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class AssociateAdminServiceTests
    Private Const GoodPassword As String = "Str0ng!Passw0rd"

    Private Shared Function Build(allowed As Boolean, ByRef repo As FakeAssociateAdminRepository, ByRef audit As FakeAuditRepository) As AssociateAdminService
        repo = New FakeAssociateAdminRepository()
        audit = New FakeAuditRepository()
        Dim session As New SessionContext()
        session.SignIn(New UserSession(5, "assoc", UserRole.Associate, 9, PlanTier.Copper))
        Dim grants As New FakePermissionRepository()
        If allowed Then
            grants.Grant(UserRole.Associate, ModuleKey.Security, PermissionAction.Create)
        End If
        Dim permissions As New PermissionService(session, grants)
        Return New AssociateAdminService(repo, permissions, New PasswordHasher(), New AuditService(audit, session, New EntitlementService(session)))
    End Function

    <Fact>
    Public Sub CreateCompany_WithoutPermission_Fails()
        Dim repo As FakeAssociateAdminRepository = Nothing
        Dim audit As FakeAuditRepository = Nothing
        Dim r = Build(False, repo, audit).CreateCompany("Gamma Foods")
        Assert.False(r.Succeeded)
        Assert.Equal(0, repo.CompaniesCreated.Count)
    End Sub

    <Fact>
    Public Sub CreateCompany_TrimsName_CreatesAndAudits()
        Dim repo As FakeAssociateAdminRepository = Nothing
        Dim audit As FakeAuditRepository = Nothing
        Dim r = Build(True, repo, audit).CreateCompany("  Gamma Foods  ")
        Assert.True(r.Succeeded)
        Assert.Equal("Gamma Foods", repo.CompaniesCreated(0))
        Assert.True(audit.Entries.Any(Function(e) e.Action = "CompanyCreated"))
    End Sub

    <Fact>
    Public Sub CreateCompany_BlankName_Fails()
        Dim repo As FakeAssociateAdminRepository = Nothing
        Dim audit As FakeAuditRepository = Nothing
        Assert.False(Build(True, repo, audit).CreateCompany("   ").Succeeded)
        Assert.Equal(0, repo.CompaniesCreated.Count)
    End Sub

    <Fact>
    Public Sub CreateOwner_ChecksNameAndPasswordPolicy_BeforeTheDatabase()
        Dim repo As FakeAssociateAdminRepository = Nothing
        Dim audit As FakeAuditRepository = Nothing
        Dim service = Build(True, repo, audit)
        Assert.False(service.CreateOwner(9, "a b", GoodPassword).Succeeded)
        Assert.False(service.CreateOwner(9, "ab", GoodPassword).Succeeded)
        Assert.False(service.CreateOwner(9, "owner9", "weak").Succeeded)
        Assert.False(service.CreateOwner(0, "owner9", GoodPassword).Succeeded)
        Assert.Equal(0, repo.OwnersCreated.Count)
    End Sub

    <Fact>
    Public Sub CreateOwner_HashesThePassword_AndAudits()
        Dim repo As FakeAssociateAdminRepository = Nothing
        Dim audit As FakeAuditRepository = Nothing
        Dim r = Build(True, repo, audit).CreateOwner(9, " owner9 ", GoodPassword)
        Assert.True(r.Succeeded)
        Assert.Equal("owner9", repo.OwnersCreated(0))
        Assert.Equal(9, repo.LastOwnerCompanyId)
        Assert.Equal(32, repo.LastHash.Length)
        Assert.True(audit.Entries.Any(Function(e) e.Action = "OwnerCreated"))
    End Sub

    <Fact>
    Public Sub CreateOwner_TakenUserName_ReturnsTheRepositoryMessage()
        Dim repo As FakeAssociateAdminRepository = Nothing
        Dim audit As FakeAuditRepository = Nothing
        Dim r = Build(True, repo, audit).CreateOwner(9, "Taken", GoodPassword)
        Assert.False(r.Succeeded)
        Assert.Equal("That user name is already taken.", r.Message)
        Assert.False(audit.Entries.Any(Function(e) e.Action = "OwnerCreated"))
    End Sub
End Class
