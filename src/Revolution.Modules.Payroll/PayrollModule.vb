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
        Dim tdsRepository As New SqlTdsRepository(host.Factory)
        Dim tdsProjection As New TdsProjectionService(host.Rates)
        Dim runRepository As New SqlPayrollRunRepository(host.Factory)
        Dim runs As New PayrollRunService(runRepository, salaryRepository, host.Rates, host.Approvals, host.Permissions, host.Entitlements, host.Audit, host.Session,
                                          New SqlRunTaxSource(host.Factory, tdsRepository), tdsProjection, New CompositeRecoveryLedger(New SqlArrearsRecoveryLedger(host.Factory), New SqlAssetRecoveryLedger(host.Factory)), New SqlRunDirectory(host.Factory))
        Dim bonus As New BonusRunService(New SqlBonusSource(host.Factory), runRepository, salaryRepository, host.Rates, host.Permissions, host.Entitlements, host.Audit, host.Session,
                                      New SqlRunTaxSource(host.Factory, tdsRepository), tdsProjection)
        Dim loans As New LoanService(New SqlLoanRepository(host.Factory), host.Permissions, host.Entitlements, host.Audit, host.Session)
        Dim tds As New TdsDeclarationService(tdsRepository, host.Crypto, host.Permissions, host.Entitlements, host.Audit, host.Session)
        Dim leaveRepository As New SqlLeaveRepository(host.Factory)
        Dim leave As New LeaveService(leaveRepository, host.Permissions, host.Entitlements, host.Audit, host.Session)
        Dim leaveAccrual As New LeaveAccrualService(New SqlLeaveAccrualSource(host.Factory), host.Permissions, host.Entitlements, host.Audit, host.Session)
        Dim fnf As New FnfService(New SqlFnfRepository(host.Factory), host.Permissions, host.Entitlements, host.Audit, host.Session,
                                  New SqlFnfFactsSource(host.Factory), host.Rates, leaveRepository, New SqlRunTaxSource(host.Factory, tdsRepository), tdsProjection, New SqlFnfAssetSource(host.Factory))
        Dim repository As New SqlPayrollFileRepository(host.Factory)
        Dim releaser As New EmployeeExportReleaser(export)
        Dim source As New PayrollFileSource(repository, releaser)
        Dim payslips As New PayslipService(New SqlPayslipRepository(host.Factory), releaser, host.Permissions, host.Entitlements, host.Audit)
        Dim files As New PayrollFileService(source, host.Permissions, host.Audit, repository)
        Dim writers As New List(Of IBankFileWriter) From {New SbiFileWriter()}
        Return New PayrollServices(salary, runs, payslips, loans, tds, tdsProjection, files, host.Rates, host.Exporter, writers, leave, fnf, leaveAccrual, bonus)
    End Function
End Class
