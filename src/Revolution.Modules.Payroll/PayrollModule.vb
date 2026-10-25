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
        Dim salaryRepository As New SqlSalaryRepository(host.Factory)
        Dim salary As New SalaryService(salaryRepository, host.Permissions, host.Entitlements, host.Audit, host.Session)
        Dim runs As New PayrollRunService(New SqlPayrollRunRepository(host.Factory), salaryRepository, host.Rates, host.Approvals, host.Permissions, host.Entitlements, host.Audit, host.Session)
        Dim loans As New LoanService(New SqlLoanRepository(host.Factory), host.Permissions, host.Entitlements, host.Audit, host.Session)
        Dim tds As New TdsDeclarationService(New SqlTdsRepository(host.Factory), host.Crypto, host.Permissions, host.Entitlements, host.Audit, host.Session)
        Dim tdsProjection As New TdsProjectionService(host.Rates)
        Dim repository As New SqlPayrollFileRepository(host.Factory)
        Dim releaser As New EmployeeExportReleaser(export)
        Dim source As New PayrollFileSource(repository, releaser)
        Dim payslips As New PayslipService(New SqlPayslipRepository(host.Factory), releaser, host.Permissions, host.Entitlements, host.Audit)
        Dim files As New PayrollFileService(source, host.Permissions, host.Audit, repository)
        Dim writers As New List(Of IBankFileWriter) From {New SbiFileWriter()}
        Return New PayrollServices(salary, runs, payslips, loans, tds, tdsProjection, files, host.Rates, host.Exporter, writers)
    End Function
End Class
