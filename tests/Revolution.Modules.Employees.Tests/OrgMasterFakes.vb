Imports Revolution.Core
Imports Revolution.Modules.Employees

Friend NotInheritable Class FakeMasterPermissions
    Implements IPermissionService

    Public ReadOnly Allowed As New HashSet(Of PermissionAction)()

    Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
        Return moduleKey = ModuleKey.Masters AndAlso Allowed.Contains(action)
    End Function
End Class

Friend NotInheritable Class FakeOrgMasterRepository
    Implements IOrgMasterRepository

    Public ReadOnly Factories As New List(Of FactoryItem)()
    Public ReadOnly Departments As New List(Of DepartmentItem)()
    Public ReadOnly Designations As New List(Of DesignationItem)()
    Public ReadOnly Banks As New List(Of BankItem)()
    Public ReadOnly States As New List(Of StateItem)()
    Public RuleError As String
    Private _nextId As Integer = 10

    Public Function ListBanks() As IReadOnlyList(Of BankItem) Implements IOrgMasterRepository.ListBanks
        Return Banks
    End Function

    Public Function ListStates() As IReadOnlyList(Of StateItem) Implements IOrgMasterRepository.ListStates
        Return States
    End Function

    Public Function ListFactories(activeOnly As Boolean) As IReadOnlyList(Of FactoryItem) Implements IOrgMasterRepository.ListFactories
        Return Factories.Where(Function(f) Not activeOnly OrElse f.IsActive).ToList()
    End Function

    Public Function SaveFactory(item As FactoryItem) As Integer Implements IOrgMasterRepository.SaveFactory
        ThrowIfRule()
        If item.FactoryId = 0 Then item.FactoryId = NextId()
        Factories.Add(item)
        Return item.FactoryId
    End Function

    Public Function ListDepartments(factoryId As Integer?, activeOnly As Boolean) As IReadOnlyList(Of DepartmentItem) Implements IOrgMasterRepository.ListDepartments
        Return Departments.Where(Function(d) (Not factoryId.HasValue OrElse d.FactoryId = factoryId.Value) AndAlso (Not activeOnly OrElse d.IsActive)).ToList()
    End Function

    Public Function SaveDepartment(item As DepartmentItem) As Integer Implements IOrgMasterRepository.SaveDepartment
        ThrowIfRule()
        If item.DepartmentId = 0 Then item.DepartmentId = NextId()
        Departments.Add(item)
        Return item.DepartmentId
    End Function

    Public Function ListDesignations(activeOnly As Boolean) As IReadOnlyList(Of DesignationItem) Implements IOrgMasterRepository.ListDesignations
        Return Designations.Where(Function(d) Not activeOnly OrElse d.IsActive).ToList()
    End Function

    Public Function SaveDesignation(item As DesignationItem) As Integer Implements IOrgMasterRepository.SaveDesignation
        ThrowIfRule()
        If item.DesignationId = 0 Then item.DesignationId = NextId()
        Designations.Add(item)
        Return item.DesignationId
    End Function

    Private Sub ThrowIfRule()
        If RuleError IsNot Nothing Then Throw New EmployeeRuleException(RuleError)
    End Sub

    Private Function NextId() As Integer
        _nextId += 1
        Return _nextId
    End Function
End Class
