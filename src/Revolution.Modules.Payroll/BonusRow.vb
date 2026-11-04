''' <summary>One employee in a bonus run preview: whether the bonus is due, why not when it is not, and the amounts. Whole rupees.</summary>
Public NotInheritable Class BonusRow
    Public Sub New(employeeId As Integer, empCode As String, result As BonusResult, daysWorked As Integer, lastMonthlyWage As Decimal,
                   Optional epfEmployee As Decimal = 0D, Optional tds As Decimal = 0D, Optional netPay As Decimal = 0D)
        If result Is Nothing Then Throw New ArgumentNullException(NameOf(result))
        Me.EmployeeId = employeeId
        Me.EmpCode = If(empCode, String.Empty)
        Me.Result = result
        Me.DaysWorked = daysWorked
        Me.LastMonthlyWage = lastMonthlyWage
        Me.EpfEmployee = epfEmployee
        Me.Tds = tds
        Me.NetPay = netPay
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property Result As BonusResult
    Public ReadOnly Property DaysWorked As Integer
    Public ReadOnly Property LastMonthlyWage As Decimal
    ''' <summary>Provident fund taken from the employee on this bonus.</summary>
    Public ReadOnly Property EpfEmployee As Decimal
    ''' <summary>Income tax taken on this bonus (the part that could be taken from it).</summary>
    Public ReadOnly Property Tds As Decimal
    ''' <summary>Paid to the employee: the bonus payable less provident fund and income tax.</summary>
    Public ReadOnly Property NetPay As Decimal

    ''' <summary>Payable now before deductions: the bonus for the year less what was already paid, never below zero.</summary>
    Public ReadOnly Property Amount As Decimal
        Get
            Return Result.Amount
        End Get
    End Property
End Class
