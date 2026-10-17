Imports Revolution.Core
Imports Revolution.Modules.Employees
Imports Xunit

Public Class OrgMasterServiceTests
    Private ReadOnly _repo As New FakeOrgMasterRepository()
    Private ReadOnly _perms As New FakeMasterPermissions()
    Private ReadOnly _audit As New FakeAudit()
    Private ReadOnly _factories As FactoryService
    Private ReadOnly _departments As DepartmentService
    Private ReadOnly _designations As DesignationService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Create)
        _perms.Allowed.Add(PermissionAction.Edit)
        Dim session As New FakeSession()
        _factories = New FactoryService(_repo, _perms, _audit, session)
        _departments = New DepartmentService(_repo, _perms, _audit, session)
        _designations = New DesignationService(_repo, _perms, _audit, session)
    End Sub

    <Fact>
    Public Sub SaveFactory_New_TrimsUppercasesAndAudits()
        Dim r = _factories.Save(New FactoryItem With {.Code = " f1 ", .Name = " Main plant ", .StateId = 6})
        Assert.True(r.Succeeded)
        Assert.Equal("F1", _repo.Factories.Single().Code)
        Assert.Equal("Main plant", _repo.Factories.Single().Name)
        Dim entry = _audit.Entries.Single()
        Assert.Equal(ModuleKey.Masters, entry.Module)
        Assert.Equal("Create", entry.Action)
        Assert.Equal("Factory", entry.EntityName)
    End Sub

    <Fact>
    Public Sub SaveFactory_Existing_NeedsEditAndAuditsUpdate()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Dim item As New FactoryItem With {.FactoryId = 4, .Code = "F1", .Name = "Main plant", .StateId = 6}
        Assert.False(_factories.Save(item).Succeeded)
        _perms.Allowed.Add(PermissionAction.Edit)
        Assert.True(_factories.Save(item).Succeeded)
        Assert.Equal("Update", _audit.Entries.Single().Action)
    End Sub

    <Fact>
    Public Sub SaveFactory_NewWithoutCreate_IsRefusedAndNothingSaved()
        _perms.Allowed.Remove(PermissionAction.Create)
        Assert.False(_factories.Save(New FactoryItem With {.Code = "F1", .Name = "Main plant", .StateId = 6}).Succeeded)
        Assert.Empty(_repo.Factories)
    End Sub

    <Fact>
    Public Sub SaveFactory_InvalidInput_ListsProblemsAndSavesNothing()
        Dim r = _factories.Save(New FactoryItem())
        Assert.False(r.Succeeded)
        Assert.Contains("Code is required.", r.Message)
        Assert.Contains("Choose the state", r.Message)
        Assert.Empty(_repo.Factories)
    End Sub

    <Fact>
    Public Sub SaveDepartment_DatabaseRule_BecomesFailedResult()
        _repo.RuleError = "This department code already exists in the factory."
        Dim r = _departments.Save(New DepartmentItem With {.FactoryId = 1, .Code = "D1", .Name = "Stitching"})
        Assert.False(r.Succeeded)
        Assert.Equal(_repo.RuleError, r.Message)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub SaveDepartment_AuditFailure_IsReported()
        _audit.FailWith = "log down"
        Dim r = _departments.Save(New DepartmentItem With {.FactoryId = 1, .Code = "D1", .Name = "Stitching"})
        Assert.False(r.Succeeded)
        Assert.Contains("was saved, but the audit entry failed", r.Message)
    End Sub

    <Fact>
    Public Sub ListDepartments_FiltersByFactory()
        _repo.Departments.Add(New DepartmentItem With {.DepartmentId = 1, .FactoryId = 1, .Code = "A", .Name = "A"})
        _repo.Departments.Add(New DepartmentItem With {.DepartmentId = 2, .FactoryId = 2, .Code = "B", .Name = "B"})
        Assert.Single(_departments.List(2, False).Value)
        Assert.Equal(2, _departments.List(Nothing, False).Value.Count)
    End Sub

    <Fact>
    Public Sub List_WithoutView_IsRefused()
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_factories.List(False).Succeeded)
        Assert.False(_departments.List(Nothing, False).Succeeded)
        Assert.False(_designations.List(False).Succeeded)
    End Sub

    <Fact>
    Public Sub SaveDesignation_Valid_ReturnsIdAndHidesInactiveWhenAsked()
        Dim r = _designations.Save(New DesignationItem With {.Code = "T1", .Name = "Tailor", .CategoryId = 3})
        Assert.True(r.Succeeded)
        _designations.Save(New DesignationItem With {.Code = "T2", .Name = "Helper", .CategoryId = 1, .IsActive = False})
        Assert.Single(_designations.List(True).Value)
        Assert.Equal(2, _designations.List(False).Value.Count)
    End Sub

    <Theory>
    <InlineData("UQ_Factory_Code")>
    <InlineData("UQ_Factory_Name")>
    <InlineData("UQ_Department_Code")>
    <InlineData("UQ_Designation_Code")>
    <InlineData("CK_Designation_Category")>
    Public Sub ErrorMap_KnowsTheMasterConstraints(constraint As String)
        Dim text = $"Violation of UNIQUE KEY constraint '{constraint}'."
        Dim message = EmployeeErrorMap.Translate(2627, text)
        Assert.DoesNotContain("This value already exists", message)
    End Sub
End Class
