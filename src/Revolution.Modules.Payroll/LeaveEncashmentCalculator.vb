Imports Revolution.Core

''' <summary>
''' Leave encashment at exit: the balance in days, cut to the most the law allows to be encashed (zero means no limit), times the daily wage, rounded once to
''' the nearest rupee, midpoint away from zero. The balance comes from the leave ledger; the limit comes from the leave rule, never from code.
''' </summary>
Public NotInheritable Class LeaveEncashmentCalculator
    Private Sub New()
    End Sub

    Public Shared Function Calculate(balanceDays As Decimal, maxEncashDays As Decimal, dailyWage As Decimal) As LeaveEncashmentResult
        If balanceDays < 0D Then Throw New ArgumentOutOfRangeException(NameOf(balanceDays), "The balance cannot be negative.")
        If maxEncashDays < 0D Then Throw New ArgumentOutOfRangeException(NameOf(maxEncashDays), "The limit cannot be negative.")
        If dailyWage < 0D Then Throw New ArgumentOutOfRangeException(NameOf(dailyWage), "The wage cannot be negative.")
        Dim capped As Boolean = maxEncashDays > 0D AndAlso balanceDays > maxEncashDays
        Dim days As Decimal = If(capped, maxEncashDays, balanceDays)
        Return New LeaveEncashmentResult(balanceDays, days, dailyWage, Rupee.Round(days * dailyWage, RateRounding.Nearest), capped)
    End Function
End Class
