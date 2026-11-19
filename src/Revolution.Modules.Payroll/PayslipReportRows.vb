''' <summary>
''' Turns payslips into the rows the payslip layout (reports/layouts/payslip.rsr, a ReportStudio Host report) is run on. One row per earning line of an employee,
''' with the employee's own columns repeated on each row; the layout groups by EmployeeID. Overtime is added as the last earning line when it is above zero
''' (it is a column of the payslip row, not a salary component). An employee with no earning line and no overtime gets one line named NoEarningsLabel so the
''' payslip still prints. Ids come from the payslip as released or masked by PayslipService: this class never decrypts anything. The key names are the layout's
''' field names; a test compares the two.
''' </summary>
Public NotInheritable Class PayslipReportRows
    Public Const OvertimeLabel As String = "Overtime"
    Public Const NoEarningsLabel As String = "No earnings this month"
    ''' <summary>Sort value of the overtime line: after every salary component.</summary>
    Public Const OvertimeSortOrder As Integer = 1000000

    Private Sub New()
    End Sub

    Public Shared Function Build(slips As IReadOnlyList(Of Payslip)) As IReadOnlyList(Of IReadOnlyDictionary(Of String, Object))
        If slips Is Nothing Then Throw New ArgumentNullException(NameOf(slips))
        Dim rows As New List(Of IReadOnlyDictionary(Of String, Object))()
        For Each slip As Payslip In slips
            Dim lines As New List(Of PayslipEarning)(slip.Row.Earnings.OrderBy(Function(e) e.SortOrder))
            If slip.Row.OvertimeAmount > 0D Then lines.Add(New PayslipEarning("OT", OvertimeLabel, OvertimeSortOrder, slip.Row.OvertimeAmount))
            If lines.Count = 0 Then lines.Add(New PayslipEarning("NONE", NoEarningsLabel, 0, 0D))
            For Each line As PayslipEarning In lines
                rows.Add(OneRow(slip, line))
            Next
        Next
        Return rows
    End Function

    ''' <summary>The report parameter ShowTds: true when the plan has the Tds feature (every payslip of one print carries the same value).</summary>
    Public Shared Function ShowTds(slips As IReadOnlyList(Of Payslip)) As Boolean
        If slips Is Nothing Then Throw New ArgumentNullException(NameOf(slips))
        Return slips.Count > 0 AndAlso slips(0).ShowTds
    End Function

    Private Shared Function OneRow(slip As Payslip, line As PayslipEarning) As IReadOnlyDictionary(Of String, Object)
        Dim r As PayslipRow = slip.Row
        Return New Dictionary(Of String, Object)(StringComparer.Ordinal) From {
            {"RunID", CDec(r.RunId)}, {"RunStatusID", CDec(CInt(r.RunStatus))}, {"PeriodFrom", r.PeriodFrom}, {"PeriodTo", r.PeriodTo},
            {"CompanyName", r.CompanyName}, {"CompanyAddress1", r.CompanyAddress1}, {"CompanyAddress2", r.CompanyAddress2}, {"FactoryName", r.FactoryName},
            {"EpfSubCode", r.EpfSubCode}, {"EsiSubCode", r.EsiSubCode}, {"EmployeeID", CDec(r.EmployeeId)}, {"EmpCode", r.EmpCode}, {"FullName", r.FullName},
            {"FatherOrSpouseName", r.FatherOrSpouseName}, {"JoinDate", r.JoinDate}, {"PfNumber", r.PfNumber},
            {"Uan", slip.Ids.Uan}, {"EsiIpNumber", slip.Ids.EsiIp}, {"BankName", r.BankName}, {"Ifsc", r.Ifsc}, {"BankAccountNumber", slip.Ids.AccountNumber},
            {"DesignationName", r.DesignationName}, {"DepartmentName", r.DepartmentName}, {"PaidDays", r.PaidDays}, {"OvertimeHours", r.OvertimeHours},
            {"OvertimeAmount", r.OvertimeAmount}, {"GrossEarnings", r.GrossEarnings}, {"EpfEmployee", r.EpfEmployee}, {"EsiEmployee", r.EsiEmployee},
            {"ProfessionalTax", r.ProfessionalTax}, {"TdsAmount", r.TdsAmount}, {"LoanEmi", r.LoanEmi}, {"OtherDeductions", r.OtherDeductions},
            {"CarryForwardIn", r.CarryForwardIn}, {"TotalDeductions", r.TotalDeductions}, {"NetPay", r.NetPay}, {"LoanEmiDeferred", r.LoanEmiDeferred},
            {"CarryForwardOut", r.CarryForwardOut}, {"ComponentName", line.Name}, {"ComponentSort", CDec(line.SortOrder)}, {"ComponentAmount", line.Amount}}
    End Function
End Class
