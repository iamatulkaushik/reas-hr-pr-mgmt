''' <summary>
''' When full and final is settled (Owner, 2026-10-27): in the month after the exit month, from the 1st to the 7th. The window is a RunPeriod so it reads like
''' the other date ranges.
''' </summary>
Public NotInheritable Class FnfDueWindow
    Private Sub New()
    End Sub

    Public Shared Function [For](exitDate As Date) As RunPeriod
        Dim nextMonth As Date = New Date(exitDate.Year, exitDate.Month, 1).AddMonths(1)
        Return New RunPeriod(nextMonth, New Date(nextMonth.Year, nextMonth.Month, 7))
    End Function

    ''' <summary>True when the pay date is inside the window for this exit date.</summary>
    Public Shared Function IsInWindow(exitDate As Date, payDate As Date) As Boolean
        Dim window As RunPeriod = [For](exitDate)
        Return payDate.Date >= window.PeriodFrom AndAlso payDate.Date <= window.PeriodTo
    End Function
End Class
