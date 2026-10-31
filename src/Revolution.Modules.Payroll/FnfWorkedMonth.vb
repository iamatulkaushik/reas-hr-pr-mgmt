''' <summary>A month worked in the accounting year, from locked or paid pay: its Basic plus DA and the paid days. Bonus is worked on these.</summary>
Public NotInheritable Class FnfWorkedMonth
    Public Sub New(monthKey As Integer, basicPlusDa As Decimal, paidDays As Decimal)
        Me.MonthKey = monthKey
        Me.BasicPlusDa = basicPlusDa
        Me.PaidDays = paidDays
    End Sub

    Public ReadOnly Property MonthKey As Integer
    Public ReadOnly Property BasicPlusDa As Decimal
    Public ReadOnly Property PaidDays As Decimal
End Class
