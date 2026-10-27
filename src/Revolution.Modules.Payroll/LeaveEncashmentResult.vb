''' <summary>Leave encashment at exit. Days is what is paid, after the cap. Amount is in whole rupees.</summary>
Public NotInheritable Class LeaveEncashmentResult
    Public Sub New(balanceDays As Decimal, days As Decimal, dailyWage As Decimal, amount As Decimal, wasCapped As Boolean)
        Me.BalanceDays = balanceDays
        Me.Days = days
        Me.DailyWage = dailyWage
        Me.Amount = amount
        Me.WasCapped = wasCapped
    End Sub

    Public ReadOnly Property BalanceDays As Decimal
    Public ReadOnly Property Days As Decimal
    Public ReadOnly Property DailyWage As Decimal
    Public ReadOnly Property Amount As Decimal
    Public ReadOnly Property WasCapped As Boolean
End Class
