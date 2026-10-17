Imports Revolution.Data

''' <summary>Composition class of the Employees module (ADR 0015). Revolution.App calls Compose once with the shared host.</summary>
Public NotInheritable Class EmployeesModule
    Private Sub New()
    End Sub

    Public Shared Function Compose(host As ModuleHost) As EmployeesServices
        If host Is Nothing Then Throw New ArgumentNullException(NameOf(host))
        Dim masters As New SqlOrgMasterRepository(host.Factory)
        Dim employees As New SqlEmployeeRepository(host.Factory)
        Dim identifiers As New EmployeeIdentifierService(employees, host.Crypto, host.Permissions, host.Audit, host.Session)
        Dim today As Func(Of Date) = Function() Date.Today
        Return New EmployeesServices(
            New FactoryService(masters, host.Permissions, host.Audit, host.Session),
            New DepartmentService(masters, host.Permissions, host.Audit, host.Session),
            New DesignationService(masters, host.Permissions, host.Audit, host.Session),
            New EmployeeService(employees, identifiers, host.Permissions, host.Audit, host.Session, today),
            identifiers,
            New EmployeeBankService(New SqlEmployeeBankRepository(host.Factory), host.Crypto, host.Permissions, host.Audit, host.Session),
            New ReferenceService(masters, host.Permissions))
    End Function
End Class
