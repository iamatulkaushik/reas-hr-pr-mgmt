''' <summary>The reports the app offers. A new report is one more entry here, one layout file, one rpt procedure and a menu position in MenuLayoutModule.</summary>
Public NotInheritable Class ReportCatalog
    Private Sub New()
    End Sub

    Public Shared ReadOnly Basic As New ReportEntry("basic", "Basic Report", "basic-report.rsr", "rpt.usp_EmployeeBasic_Get", ReportInput.EmployeeList)
    Public Shared ReadOnly SalarySheet As New ReportEntry("salarysheet", "Salary Sheet", "salary-sheet.rsr", "rpt.usp_WageRegister_Get", ReportInput.PayrollRun)
    Public Shared ReadOnly WageRegister As New ReportEntry("wageregister", "Wage Register", "wage-register.rsr", "rpt.usp_WageRegister_Get", ReportInput.PayrollRun)
    Public Shared ReadOnly PayrollAbstract As New ReportEntry("abstract", "Abstract", "payroll-summary.rsr", "rpt.usp_PayrollSummary_Get", ReportInput.PayrollRun)
    Public Shared ReadOnly Muster As New ReportEntry("muster", "Muster Roll", "muster-roll.rsr", "rpt.usp_Muster_Get", ReportInput.Month)

    Public Shared ReadOnly All As IReadOnlyList(Of ReportEntry) = New List(Of ReportEntry) From {Basic, SalarySheet, WageRegister, PayrollAbstract, Muster}
End Class
