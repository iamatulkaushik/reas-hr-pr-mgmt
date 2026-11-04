''' <summary>One employee's month in the accounting year, from a locked or paid regular run: Basic plus DA earned, and the days paid.</summary>
Public NotInheritable Class BonusMonthFact
    Public Sub New(employeeId As Integer, empCode As String, factoryId As Integer, monthKey As Integer, basicPlusDa As Decimal, paidDays As Decimal)
        Me.EmployeeId = employeeId
        Me.EmpCode = If(empCode, String.Empty)
        Me.FactoryId = factoryId
        Me.MonthKey = monthKey
        Me.BasicPlusDa = basicPlusDa
        Me.PaidDays = paidDays
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property FactoryId As Integer
    ''' <summary>Year and month of the run's period end, as yyyymm.</summary>
    Public ReadOnly Property MonthKey As Integer
    Public ReadOnly Property BasicPlusDa As Decimal
    Public ReadOnly Property PaidDays As Decimal
End Class
