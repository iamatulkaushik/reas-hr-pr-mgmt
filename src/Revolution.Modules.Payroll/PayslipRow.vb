''' <summary>
''' One employee's payslip data as read from rpt.usp_Payslip_Get. No id is here in clear or in cipher: only the last four characters. Strings may be empty when
''' the employee has no value. Run-level fields repeat on every row.
''' </summary>
Public NotInheritable Class PayslipRow
    Public Property RunId As Integer
    Public Property RunStatus As PayrollRunStatus
    Public Property PeriodFrom As Date
    Public Property PeriodTo As Date
    Public Property CompanyName As String = String.Empty
    Public Property CompanyAddress1 As String = String.Empty
    Public Property CompanyAddress2 As String = String.Empty
    Public Property FactoryName As String = String.Empty
    Public Property EpfSubCode As String = String.Empty
    Public Property EsiSubCode As String = String.Empty
    Public Property EmployeeId As Integer
    Public Property EmpCode As String = String.Empty
    Public Property FullName As String = String.Empty
    Public Property FatherOrSpouseName As String = String.Empty
    Public Property JoinDate As Date
    Public Property PfNumber As String = String.Empty
    Public Property UanLast4 As String = String.Empty
    Public Property EsiIpLast4 As String = String.Empty
    Public Property BankName As String = String.Empty
    Public Property Ifsc As String = String.Empty
    Public Property AccountLast4 As String = String.Empty
    Public Property DesignationName As String = String.Empty
    Public Property DepartmentName As String = String.Empty
    Public Property PaidDays As Decimal
    Public Property OvertimeHours As Decimal
    Public Property OvertimeAmount As Decimal
    Public Property GrossEarnings As Decimal
    Public Property EpfEmployee As Decimal
    Public Property EsiEmployee As Decimal
    Public Property ProfessionalTax As Decimal
    Public Property LoanEmi As Decimal
    Public Property OtherDeductions As Decimal
    Public Property CarryForwardIn As Decimal
    Public Property TotalDeductions As Decimal
    Public Property NetPay As Decimal
    Public Property LoanEmiDeferred As Decimal
    Public Property CarryForwardOut As Decimal
    Public Property Earnings As IReadOnlyList(Of PayslipEarning) = New List(Of PayslipEarning)()
End Class
