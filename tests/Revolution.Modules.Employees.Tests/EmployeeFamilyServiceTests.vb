Imports Revolution.Core
Imports Revolution.Modules.Employees
Imports Xunit

Public Class EmployeeFamilyServiceTests
    Private ReadOnly _repo As New FakeFamilyRepository()
    Private ReadOnly _perms As New FakeEmployeePermissions()
    Private ReadOnly _audit As New FakeAudit()
    Private ReadOnly _service As EmployeeFamilyService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Edit)
        _service = New EmployeeFamilyService(_repo, _perms, _audit, Function() New Date(2026, 10, 17))
    End Sub

    <Fact>
    Public Sub SaveFamily_New_TrimsAndAudits()
        Dim r = _service.SaveFamily(7, New FamilyMember With {.Name = " Sunita ", .Relation = " Spouse "})
        Assert.True(r.Succeeded)
        Assert.Equal("Sunita", _repo.Family.Single().Name)
        Assert.Equal("Spouse", _repo.Family.Single().Relation)
        Assert.Equal("AddFamily", _audit.Entries.Single().Action)
    End Sub

    <Fact>
    Public Sub SaveFamily_Existing_AuditsUpdate()
        _service.SaveFamily(7, New FamilyMember With {.FamilyId = 3, .Name = "Sunita", .Relation = "Spouse"})
        Assert.Equal("UpdateFamily", _audit.Entries.Single().Action)
    End Sub

    <Fact>
    Public Sub Changes_WithoutEdit_AreRefusedAndNothingChanges()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.SaveFamily(7, New FamilyMember With {.Name = "A", .Relation = "B"}).Succeeded)
        Assert.False(_service.DeleteFamily(7, 3).Succeeded)
        Assert.False(_service.SaveNominee(7, NomineeScheme.Epf, 3, 50D).Succeeded)
        Assert.False(_service.DeleteNominee(7, 3).Succeeded)
        Assert.Empty(_repo.Family)
        Assert.Empty(_repo.Nominees)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub Lists_WithoutView_AreRefused()
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.ListFamily(7).Succeeded)
        Assert.False(_service.ListNominees(7).Succeeded)
    End Sub

    <Fact>
    Public Sub Lists_NeedAnEmployee()
        Assert.False(_service.ListFamily(0).Succeeded)
        Assert.False(_service.ListNominees(0).Succeeded)
    End Sub

    <Fact>
    Public Sub SaveFamily_InvalidInput_ListsProblems()
        Dim r = _service.SaveFamily(7, New FamilyMember())
        Assert.False(r.Succeeded)
        Assert.Contains("Name is required.", r.Message)
        Assert.Contains("Relation is required.", r.Message)
        Assert.Empty(_repo.Family)
    End Sub

    <Fact>
    Public Sub DeleteFamily_StillANominee_ShowsTheDatabaseMessage()
        _repo.RuleError = "This person is still a nominee. Remove the nomination first."
        Dim r = _service.DeleteFamily(7, 3)
        Assert.False(r.Succeeded)
        Assert.Equal(_repo.RuleError, r.Message)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub SaveNominee_Valid_SavesAndAudits()
        Dim r = _service.SaveNominee(7, NomineeScheme.Gratuity, 3, 60.5D)
        Assert.True(r.Succeeded)
        Assert.Equal(60.5D, _repo.Nominees.Single().SharePercent)
        Assert.Equal("SaveNominee", _audit.Entries.Single().Action)
    End Sub

    <Fact>
    Public Sub SaveNominee_ShareAbove100_IsRefused()
        Assert.False(_service.SaveNominee(7, NomineeScheme.Epf, 3, 100.5D).Succeeded)
        Assert.Empty(_repo.Nominees)
    End Sub

    <Fact>
    Public Sub SaveNominee_DatabaseTotalRule_BecomesFailedResult()
        _repo.RuleError = "Nominee shares for this scheme cannot add up to more than 100 percent."
        Dim r = _service.SaveNominee(7, NomineeScheme.Epf, 3, 60D)
        Assert.False(r.Succeeded)
        Assert.Equal(_repo.RuleError, r.Message)
    End Sub

    <Fact>
    Public Sub DeleteNominee_Valid_Audits()
        Assert.True(_service.DeleteNominee(7, 4).Succeeded)
        Assert.Equal("DeleteNominee", _audit.Entries.Single().Action)
    End Sub
End Class
