''' <summary>
''' The pay period of a wage month. Start day 1 is the calendar month. Start day 26 runs from the 26th of the month before to the 25th of the wage month.
''' The wage month is the month of the period end date.
''' </summary>
Public NotInheritable Class PayCycle
    Private Sub New()
    End Sub

    Public Shared Function PeriodFor(year As Integer, month As Integer, startDay As Integer) As RunPeriod
        If year < 2000 OrElse year > 9999 Then Throw New ArgumentOutOfRangeException(NameOf(year))
        If month < 1 OrElse month > 12 Then Throw New ArgumentOutOfRangeException(NameOf(month))
        Select Case startDay
            Case 1
                Return New RunPeriod(New Date(year, month, 1), New Date(year, month, Date.DaysInMonth(year, month)))
            Case 26
                Dim previous As Date = New Date(year, month, 1).AddMonths(-1)
                Return New RunPeriod(New Date(previous.Year, previous.Month, 26), New Date(year, month, 25))
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(startDay), "Pay cycle starts on the 1st or the 26th.")
        End Select
    End Function
End Class
