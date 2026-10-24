Imports Revolution.Data

''' <summary>
''' Composition class of the Payroll module (ADR 0015). Revolution.App calls Compose once with the shared host and the
''' Employees export service: PayrollModule.Compose(services.Host, employees.Export).
''' </summary>
Public NotInheritable Class PayrollModule
    Private Sub New()
    End Sub

    Public Shared Function Compose(host As ModuleHost, export As Revolution.Modules.Employees.EmployeeExportService) As PayrollServices
        If host Is Nothing Then Throw New ArgumentNullException(NameOf(host))
        If export Is Nothing Then Throw New ArgumentNullException(NameOf(export))
        Dim salary As New SalaryService(New SqlSalaryRepository(host.Factory), host.Permissions, host.Entitlements, host.Audit, host.Session)
        Dim repository As New SqlPayrollFileRepository(host.Factory)
        Dim source As New PayrollFileSource(repository, New EmployeeExportReleaser(export))
        Dim files As New PayrollFileService(source, host.Permissions, host.Audit, repository)
        Dim writers As New List(Of IBankFileWriter) From {New SbiFileWriter()}
        Return New PayrollServices(salary, files, host.Rates, host.Exporter, writers)
    End Function
End Class
