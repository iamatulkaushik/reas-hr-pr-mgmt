''' <summary>The services of the Employees module, built once by EmployeesModule.Compose and handed to its forms.</summary>
Public NotInheritable Class EmployeesServices
    Public Sub New(factories As FactoryService, departments As DepartmentService, designations As DesignationService,
                   employees As EmployeeService, identifiers As EmployeeIdentifierService, bank As EmployeeBankService,
                   reference As ReferenceService, family As EmployeeFamilyService)
        If factories Is Nothing Then Throw New ArgumentNullException(NameOf(factories))
        If departments Is Nothing Then Throw New ArgumentNullException(NameOf(departments))
        If designations Is Nothing Then Throw New ArgumentNullException(NameOf(designations))
        If employees Is Nothing Then Throw New ArgumentNullException(NameOf(employees))
        If identifiers Is Nothing Then Throw New ArgumentNullException(NameOf(identifiers))
        If bank Is Nothing Then Throw New ArgumentNullException(NameOf(bank))
        If reference Is Nothing Then Throw New ArgumentNullException(NameOf(reference))
        If family Is Nothing Then Throw New ArgumentNullException(NameOf(family))
        Me.Factories = factories
        Me.Departments = departments
        Me.Designations = designations
        Me.Employees = employees
        Me.Identifiers = identifiers
        Me.Bank = bank
        Me.Reference = reference
        Me.Family = family
    End Sub

    Public ReadOnly Property Factories As FactoryService
    Public ReadOnly Property Departments As DepartmentService
    Public ReadOnly Property Designations As DesignationService
    Public ReadOnly Property Employees As EmployeeService
    Public ReadOnly Property Identifiers As EmployeeIdentifierService
    Public ReadOnly Property Bank As EmployeeBankService
    Public ReadOnly Property Reference As ReferenceService
    Public ReadOnly Property Family As EmployeeFamilyService
End Class
