Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class ProvisioningServiceTests
    Private Const GoodPassword As String = "Str0ng!Passw0rd"
    Private ReadOnly _users As New FakeUserRepository()
    Private ReadOnly _companies As New FakeCompanyRepository()
    Private ReadOnly _repo As New FakeProvisioningRepository()
    Private ReadOnly _hasher As New PasswordHasher()
    Private ReadOnly _svc As ProvisioningService

    Public Sub New()
        _companies.Plans(1) = PlanTier.Gold
        _svc = New ProvisioningService(_users, _companies, _repo, _hasher)
    End Sub

    <Fact>
    Public Sub CreateUser_Owner_HashesPasswordAndForcesChange()
        Dim r = _svc.CreateUser("owner1", GoodPassword, UserRole.Owner, 1)
        Assert.True(r.Succeeded)
        Assert.True(_repo.LastMustChange)
        Assert.True(_hasher.Verify(GoodPassword, _repo.LastHash, _repo.LastSalt, _repo.LastIterations))
    End Sub

    <Fact>
    Public Sub CreateUser_Developer_WithoutCompany_Succeeds()
        Assert.True(_svc.CreateUser("dev", GoodPassword, UserRole.Developer, Nothing).Succeeded)
    End Sub

    <Fact>
    Public Sub CreateUser_Developer_WithCompany_Fails()
        Assert.False(_svc.CreateUser("dev", GoodPassword, UserRole.Developer, 1).Succeeded)
        Assert.Equal(0, _repo.CreatedUsers)
    End Sub

    <Fact>
    Public Sub CreateUser_Owner_WithoutCompany_Fails()
        Assert.False(_svc.CreateUser("owner1", GoodPassword, UserRole.Owner, Nothing).Succeeded)
    End Sub

    <Fact>
    Public Sub CreateUser_UnknownCompany_Fails()
        Assert.False(_svc.CreateUser("owner1", GoodPassword, UserRole.Owner, 99).Succeeded)
    End Sub

    <Fact>
    Public Sub CreateUser_WeakPassword_Fails()
        Assert.False(_svc.CreateUser("owner1", "weak", UserRole.Owner, 1).Succeeded)
        Assert.Equal(0, _repo.CreatedUsers)
    End Sub

    <Fact>
    Public Sub CreateUser_DuplicateName_Fails()
        _users.Add(TestData.User("owner1", GoodPassword, UserRole.Owner, 1))
        Assert.False(_svc.CreateUser("OWNER1", GoodPassword, UserRole.Owner, 1).Succeeded)
    End Sub

    <Fact>
    Public Sub CreateCompany_NeedsNameAndKnownPlan()
        Assert.False(_svc.CreateCompany(" ", PlanTier.Copper).Succeeded)
        Assert.False(_svc.CreateCompany("Acme", CType(99, PlanTier)).Succeeded)
        Assert.True(_svc.CreateCompany("Acme", PlanTier.Silver).Succeeded)
    End Sub

    <Fact>
    Public Sub SetAssociatePlan_OnlyForAnExistingAssociate()
        Dim assoc = TestData.User("assoc1", GoodPassword, UserRole.Associate, Nothing)
        Dim owner = TestData.User("owner1", GoodPassword, UserRole.Owner, 1)
        _users.Add(assoc)
        _users.Add(owner)
        Assert.True(_svc.SetAssociatePlan(" assoc1 ", PlanTier.Gold).Succeeded)
        Assert.Equal(assoc.UserId, _repo.LastAssociatePlanUserId)
        Assert.Equal(PlanTier.Gold, _repo.LastAssociatePlan)
        Assert.False(_svc.SetAssociatePlan("owner1", PlanTier.Gold).Succeeded)
        Assert.False(_svc.SetAssociatePlan("nobody", PlanTier.Gold).Succeeded)
    End Sub

    <Fact>
    Public Sub CreateUser_Associate_MayStartWithoutACompany()
        Assert.True(_svc.CreateUser("assoc2", GoodPassword, UserRole.Associate, Nothing).Succeeded)
        Assert.False(_svc.CreateUser("assoc3", GoodPassword, UserRole.Associate, 99).Succeeded)
    End Sub
End Class
