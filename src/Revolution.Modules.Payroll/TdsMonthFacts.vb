''' <summary>One month of the financial year for projection: salary for HRA (basic plus DA), HRA paid, and the taxable salary of the month (earned for past months, projected for the rest).</summary>
Public NotInheritable Class TdsMonthFacts
    Public Sub New(monthKey As Integer, basicPlusDa As Decimal, hraReceived As Decimal, taxableSalary As Decimal)
        Me.MonthKey = monthKey
        Me.BasicPlusDa = basicPlusDa
        Me.HraReceived = hraReceived
        Me.TaxableSalary = taxableSalary
    End Sub

    ''' <summary>YYYYMM.</summary>
    Public ReadOnly Property MonthKey As Integer
    Public ReadOnly Property BasicPlusDa As Decimal
    Public ReadOnly Property HraReceived As Decimal
    Public ReadOnly Property TaxableSalary As Decimal
End Class
