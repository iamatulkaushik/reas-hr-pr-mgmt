Imports Revolution.Data
Imports Revolution.Modules.Payroll

''' <summary>Composition class of the Arrears module (ADR 0015). Revolution.App calls ArrearsModule.Compose(services.Host) once.</summary>
Public NotInheritable Class ArrearsModule
    Private Sub New()
    End Sub

    Public Shared Function Compose(host As ModuleHost) As ArrearsServices
        Return Compose(host, Nothing)
    End Function

    ''' <summary>With the Employees export the module can build the arrear ECR (UAN released through ADR 0019). Without it the ECR button is hidden.</summary>
    Public Shared Function Compose(host As ModuleHost, export As Revolution.Modules.Employees.EmployeeExportService) As ArrearsServices
        If host Is Nothing Then Throw New ArgumentNullException(NameOf(host))
        Dim service As New ArrearsService(New SqlArrearsRepository(host.Factory), host.Permissions, host.Entitlements, host.Audit, host.Session)
        Dim prepare As New ArrearsPrepareService(New SqlArrearsSource(host.Factory), New SqlPayrollRunRepository(host.Factory), New SqlSalaryRepository(host.Factory),
                                                 New HostArrearsRates(host.Rates), host.Permissions, host.Entitlements)
        Dim ecr As ArrearsEcrService = Nothing
        If export IsNot Nothing Then
            ecr = New ArrearsEcrService(New SqlArrearsRepository(host.Factory), New EmployeeExportReleaser(export), host.Permissions, host.Entitlements, host.Audit)
        End If
        Return New ArrearsServices(service, prepare, ecr)
    End Function
End Class
