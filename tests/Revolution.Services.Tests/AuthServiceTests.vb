Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class AuthServiceTests
    Private Const GoodPassword As String = "Str0ng!Passw0rd"
    Private Const NewPassword As String = "N3w!Passw0rd#2"
    Private ReadOnly _f As New AuthFixture()

    Private Function AddOwner(Optional mustChange As Boolean = False, Optional lockedUntil As DateTime? = Nothing) As UserRecord
        Dim u = TestData.User("owner1", GoodPassword, UserRole.Owner, 7, mustChange, lockedUntil)
        _f.Users.Add(u)
        _f.Companies.Plans(7) = PlanTier.Gold
        Return u
    End Function

    <Fact>
    Public Sub Login_Success_SignsInWithCompanyPlan()
        Dim u = AddOwner()
        Dim r = _f.Service.Login("owner1", GoodPassword)
        Assert.True(r.Succeeded)
        Assert.True(_f.Session.IsAuthenticated)
        Assert.Equal(PlanTier.Gold, _f.Session.Current.Plan)
        Assert.Equal(7, _f.Session.Current.CompanyId)
        Assert.True(_f.Users.SuccessfulLogins.Contains(u.UserId))
        Assert.True(_f.AuditRows.Entries.Any(Function(e) e.Action = "LoginSucceeded"))
    End Sub

    <Fact>
    Public Sub Login_WrongPassword_RecordsFailureAndDoesNotSignIn()
        Dim u = AddOwner()
        Dim r = _f.Service.Login("owner1", "wrong")
        Assert.False(r.Succeeded)
        Assert.False(_f.Session.IsAuthenticated)
        Assert.True(_f.Users.FailedLogins.Contains(u.UserId))
        Assert.True(_f.AuditRows.Entries.Any(Function(e) e.Action = "LoginFailed"))
    End Sub

    <Fact>
    Public Sub Login_UnknownUser_GivesSameMessageAsWrongPassword()
        AddOwner()
        Dim unknown = _f.Service.Login("nobody", GoodPassword)
        Dim wrong = _f.Service.Login("owner1", "wrong")
        Assert.False(unknown.Succeeded)
        Assert.Equal(wrong.Message, unknown.Message)
        Assert.Empty(_f.Users.FailedLogins.Where(Function(id) id = 0))
    End Sub

    <Fact>
    Public Sub Login_LockedAccount_FailsWithoutCountingAnotherFailure()
        AddOwner(lockedUntil:=_f.Time.GetUtcNow().UtcDateTime.AddMinutes(10))
        Dim r = _f.Service.Login("owner1", GoodPassword)
        Assert.False(r.Succeeded)
        Assert.False(_f.Session.IsAuthenticated)
        Assert.Empty(_f.Users.FailedLogins)
    End Sub

    <Fact>
    Public Sub Login_ExpiredLock_Succeeds()
        AddOwner(lockedUntil:=_f.Time.GetUtcNow().UtcDateTime.AddMinutes(-1))
        Assert.True(_f.Service.Login("owner1", GoodPassword).Succeeded)
    End Sub

    <Fact>
    Public Sub Login_Developer_HasNoCompanyAndTopPlan()
        _f.Users.Add(TestData.User("dev", GoodPassword, UserRole.Developer, Nothing))
        Dim r = _f.Service.Login("dev", GoodPassword)
        Assert.True(r.Succeeded)
        Assert.Equal(0, r.Value.CompanyId)
        Assert.Equal(PlanTier.Platinum, r.Value.Plan)
    End Sub

    <Fact>
    Public Sub Login_CompanyWithoutLicence_Fails()
        _f.Users.Add(TestData.User("orphan", GoodPassword, UserRole.Operator, 9))
        Assert.False(_f.Service.Login("orphan", GoodPassword).Succeeded)
        Assert.False(_f.Session.IsAuthenticated)
    End Sub

    <Fact>
    Public Sub Login_MustChangePassword_FlagsSessionAndBlocksPermissions()
        AddOwner(mustChange:=True)
        Dim repo As New FakePermissionRepository()
        repo.Grant(UserRole.Owner, ModuleKey.Employees, PermissionAction.View)
        Dim perms As New PermissionService(_f.Session, repo)
        Assert.True(_f.Service.Login("owner1", GoodPassword).Succeeded)
        Assert.True(_f.Session.Current.MustChangePassword)
        Assert.False(perms.IsAllowed(ModuleKey.Employees, PermissionAction.View))
    End Sub

    <Fact>
    Public Sub ChangePassword_Success_ClearsFlagAndNewPasswordWorks()
        AddOwner(mustChange:=True)
        _f.Service.Login("owner1", GoodPassword)
        Dim r = _f.Service.ChangePassword("owner1", GoodPassword, NewPassword)
        Assert.True(r.Succeeded)
        Assert.False(_f.Session.Current.MustChangePassword)
        _f.Session.SignOut()
        Assert.True(_f.Service.Login("owner1", NewPassword).Succeeded)
        Assert.False(_f.Session.Current.MustChangePassword)
    End Sub

    <Fact>
    Public Sub ChangePassword_WeakPassword_Fails()
        AddOwner()
        Assert.False(_f.Service.ChangePassword("owner1", GoodPassword, "short").Succeeded)
    End Sub

    <Fact>
    Public Sub ChangePassword_SamePassword_Fails()
        AddOwner()
        Assert.False(_f.Service.ChangePassword("owner1", GoodPassword, GoodPassword).Succeeded)
    End Sub

    <Fact>
    Public Sub ChangePassword_WrongCurrent_CountsAsFailedLogin()
        Dim u = AddOwner()
        Assert.False(_f.Service.ChangePassword("owner1", "wrong", NewPassword).Succeeded)
        Assert.True(_f.Users.FailedLogins.Contains(u.UserId))
    End Sub
End Class
