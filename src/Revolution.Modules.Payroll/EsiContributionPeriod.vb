''' <summary>
''' ESI contribution periods: 1 April to 30 September and 1 October to 31 March. An employee covered in an earlier month of a period stays covered until it
''' ends (CR 0031). pay.usp_RunInput_List works the same period start in SQL (migration 0345); this class is the tested statement of the rule.
''' </summary>
Public NotInheritable Class EsiContributionPeriod
    Private Sub New()
    End Sub

    ''' <summary>First day of the contribution period the date falls in: 1 April or 1 October.</summary>
    Public Shared Function StartOf(day As Date) As Date
        If day.Month >= 4 AndAlso day.Month <= 9 Then Return New Date(day.Year, 4, 1)
        If day.Month >= 10 Then Return New Date(day.Year, 10, 1)
        Return New Date(day.Year - 1, 10, 1)
    End Function

    ''' <summary>Last day of the contribution period the date falls in: 30 September or 31 March.</summary>
    Public Shared Function EndOf(day As Date) As Date
        Dim start As Date = StartOf(day)
        Return start.AddMonths(6).AddDays(-1)
    End Function

    Public Shared Function SamePeriod(first As Date, second As Date) As Boolean
        Return StartOf(first) = StartOf(second)
    End Function
End Class
