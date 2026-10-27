Imports Revolution.Core

''' <summary>
''' Notice pay recovery (Owner, 2026-10-27): the notice period comes from the employee's separation record, served days run from the notice date to the last
''' working day (the last day marked as resigned), and the daily wage is the gross wage. Recovery is the short days times the daily wage, rounded once to the
''' nearest rupee, midpoint away from zero. The caller works out the daily wage (gross divided by the pay-cycle divisor for a monthly employee, the daily rate
''' for a daily wager).
''' </summary>
Public NotInheritable Class NoticePayCalculator
    Private Sub New()
    End Sub

    ''' <summary>Days from the notice date to the last working day, both counted. Zero when there is no notice date or the dates are out of order.</summary>
    Public Shared Function ServedDays(noticeDate As Date?, lastWorkingDay As Date) As Integer
        If Not noticeDate.HasValue OrElse lastWorkingDay.Date < noticeDate.Value.Date Then Return 0
        Return CInt((lastWorkingDay.Date - noticeDate.Value.Date).TotalDays) + 1
    End Function

    Public Shared Function Calculate(noticePeriodDays As Integer, servedDays As Integer, dailyWage As Decimal) As NoticePayResult
        If noticePeriodDays < 0 Then Throw New ArgumentOutOfRangeException(NameOf(noticePeriodDays), "Notice days cannot be negative.")
        If servedDays < 0 Then Throw New ArgumentOutOfRangeException(NameOf(servedDays), "Served days cannot be negative.")
        If dailyWage < 0D Then Throw New ArgumentOutOfRangeException(NameOf(dailyWage), "The wage cannot be negative.")
        Dim shortDays As Integer = Math.Max(0, noticePeriodDays - servedDays)
        Dim amount As Decimal = Rupee.Round(shortDays * dailyWage, RateRounding.Nearest)
        Return New NoticePayResult(noticePeriodDays, servedDays, shortDays, dailyWage, amount)
    End Function
End Class
