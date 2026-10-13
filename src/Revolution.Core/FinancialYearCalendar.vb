''' <summary>The Indian financial year runs 1 April to 31 March. It is identified by the year it starts in.</summary>
Public NotInheritable Class FinancialYearCalendar
    Private Sub New()
    End Sub

    ''' <summary>31-03-2027 and 01-04-2026 both give 2026.</summary>
    Public Shared Function StartYear(day As Date) As Integer
        Return If(day.Month >= 4, day.Year, day.Year - 1)
    End Function

    ''' <summary>The label printed on forms, for example 2026-27.</summary>
    Public Shared Function Label(startYear As Integer) As String
        Return startYear.ToString(Globalization.CultureInfo.InvariantCulture) & "-" &
               ((startYear + 1) Mod 100).ToString("00", Globalization.CultureInfo.InvariantCulture)
    End Function
End Class
