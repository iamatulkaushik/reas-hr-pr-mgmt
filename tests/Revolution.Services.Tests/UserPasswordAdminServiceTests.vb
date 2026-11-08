Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class UserPasswordAdminServiceTests
    Private Const GoodPassword As String = "Str0ng!Passw0rd"

    Private Shared Function Build(allowed As Boolean, ByRef repo As FakeUserPasswordAdminRepository, ByRef audit As FakeAuditRepository) As UserPasswordAdminService
        repo = New FakeUserPasswordAdminRepository()
        audit = New FakeAuditRepository()
        Dim session As New SessionContext()
        session.SignIn(New UserSession(5, "assoc", UserRole.Associate, 9, PlanTier.Copper))
        Dim grants As New FakePermissionRepository()
        If allowed Then
            grants.Grant(UserRole.Associate, ModuleKey.Security, PermissionAction.Edit)
        End If
        Dim permissions As New PermissionService(session, grants)
        Return New UserPasswordAdminService(repo, permissions, New PasswordHasher(), New AuditService(audit, session, New EntitlementService(session)))
    End Function

    <Fact>
    Public Sub WithoutEditOnSecurity_ListAndResetAreRefused()
        Dim repo As FakeUserPasswordAdminRepository = Nothing
        Dim audit As FakeAuditRepository = Nothing
        Dim service = Build(False, repo, audit)
        Assert.False(service.ListResettable().Succeeded)
        Assert.False(service.ResetPassword(12, GoodPassword).Succeeded)
        Assert.Empty(repo.ResetCalls)
        Assert.Empty(audit.Entries)
    End Sub

    <Fact>
    Public Sub List_PassesTheDatabaseListThrough()
        Dim repo As FakeUserPasswordAdminRepository = Nothing
        Dim audit As FakeAuditRepository = Nothing
        Dim service = Build(True, repo, audit)
        repo.Users.Add(New ResettableUser(12, "owner9", UserRole.Owner, False, False))
        Dim r = service.ListResettable()
        Assert.True(r.Succeeded)
        Assert.Equal("owner9", r.Value.Single().UserName)
    End Sub

    <Fact>
    Public Sub Reset_ChecksTheTargetAndThePasswordPolicy_BeforeTheDatabase()
        Dim repo As FakeUserPasswordAdminRepository = Nothing
        Dim audit As FakeAuditRepository = Nothing
        Dim service = Build(True, repo, audit)
        Assert.False(service.ResetPassword(0, GoodPassword).Succeeded)
        Assert.False(service.ResetPassword(12, "weak").Succeeded)
        Assert.False(service.ResetPassword(12, Nothing).Succeeded)
        Assert.Empty(repo.ResetCalls)
    End Sub

    <Fact>
    Public Sub Reset_HashesThePassword_PassesNoPlainText_AndAudits()
        Dim repo As FakeUserPasswordAdminRepository = Nothing
        Dim audit As FakeAuditRepository = Nothing
        Dim r = Build(True, repo, audit).ResetPassword(12, GoodPassword)
        Assert.True(r.Succeeded)
        Assert.Equal(12, repo.ResetCalls.Single())
        Assert.True(New PasswordHasher().Verify(GoodPassword, repo.LastHash, repo.LastSalt, repo.LastIterations))
        Dim entry = audit.Entries.Single(Function(e) e.Action = "PasswordReset")
        Assert.Equal("12", entry.EntityId)
        Assert.DoesNotContain(GoodPassword, entry.Details)
    End Sub

    <Fact>
    Public Sub Reset_WhenTheDatabaseRefuses_ReturnsItsMessage_AndDoesNotAudit()
        Dim repo As FakeUserPasswordAdminRepository = Nothing
        Dim audit As FakeAuditRepository = Nothing
        Dim service = Build(True, repo, audit)
        repo.FailWith = "You cannot reset the password of that user."
        Dim r = service.ResetPassword(12, GoodPassword)
        Assert.False(r.Succeeded)
        Assert.Equal("You cannot reset the password of that user.", r.Message)
        Assert.Empty(audit.Entries)
    End Sub
End Class
