Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class AuditServiceTests
    Private ReadOnly _session As New SessionContext()
    Private ReadOnly _repo As New FakeAuditRepository()
    Private ReadOnly _svc As AuditService

    Public Sub New()
        _svc = New AuditService(_repo, _session, New EntitlementService(_session))
    End Sub

    Private Shared Function EmployeeEvent() As AuditEntry
        Return New AuditEntry(ModuleKey.Employees, "Edit", "Employee", "12", "Changed grade")
    End Function

    <Fact>
    Public Sub Copper_SkipsBusinessEvents()
        _session.SignIn(TestData.Session(UserRole.Owner, PlanTier.Copper))
        Assert.True(_svc.Record(EmployeeEvent()).Succeeded)
        Assert.Empty(_repo.Entries)
    End Sub

    <Fact>
    Public Sub Copper_StillKeepsSecurityEvents()
        _session.SignIn(TestData.Session(UserRole.Owner, PlanTier.Copper))
        _svc.Record(New AuditEntry(ModuleKey.Security, "PasswordChanged", "User", "tester"))
        Assert.Single(_repo.Entries)
    End Sub

    <Fact>
    Public Sub Platinum_KeepsBusinessEvents_WithUserAndCompany()
        _session.SignIn(TestData.Session(UserRole.Owner, PlanTier.Platinum))
        _svc.Record(EmployeeEvent())
        Assert.Single(_repo.Entries)
        Assert.Equal(100, _repo.LastUserId)
        Assert.Equal(1, _repo.LastCompanyId)
    End Sub

    <Fact>
    Public Sub Developer_EventsHaveNoCompany()
        _session.SignIn(TestData.Session(UserRole.Developer, PlanTier.Platinum))
        _svc.Record(New AuditEntry(ModuleKey.Licence, "AssignPlan", "Company", "1"))
        Assert.False(_repo.LastCompanyId.HasValue)
    End Sub

    <Fact>
    Public Sub NotSignedIn_SecurityEventHasNoUser()
        _svc.Record(New AuditEntry(ModuleKey.Security, "LoginFailed", "User", "nobody"))
        Assert.Single(_repo.Entries)
        Assert.False(_repo.LastUserId.HasValue)
    End Sub
End Class
