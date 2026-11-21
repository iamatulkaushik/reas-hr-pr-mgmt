''' <summary>
''' Counts that the company rules give when the Associate's sheet leaves a column blank (ADR 0026): weekly off days from the weekly-off rule,
''' holidays from the holiday list. A holiday on a weekly off day is counted once, as weekly off.
''' </summary>
Public NotInheritable Class MonthlyRules
    Private Sub New()
    End Sub

    ''' <summary>The weekly-off rule in force on a day: the one with the latest EffectiveFrom on or before it; Nothing when none applies.</summary>
    Public Shared Function RuleOn(day As Date, rules As IEnumerable(Of WeeklyOffRule)) As WeeklyOffRule
        If rules Is Nothing Then Throw New ArgumentNullException(NameOf(rules))
        Dim found As WeeklyOffRule = Nothing
        For Each rule In rules
            If rule.EffectiveFrom <= day.Date AndAlso (found Is Nothing OrElse rule.EffectiveFrom > found.EffectiveFrom) Then
                found = rule
            End If
        Next
        Return found
    End Function

    Public Shared Function WeeklyOffDays(fromDay As Date, toDay As Date, rules As IEnumerable(Of WeeklyOffRule)) As Integer
        Dim count As Integer = 0
        Dim list = rules.ToList()
        Dim day As Date = fromDay.Date
        While day <= toDay.Date
            Dim rule = RuleOn(day, list)
            If rule IsNot Nothing AndAlso rule.IsOff(day.DayOfWeek) Then count += 1
            day = day.AddDays(1)
        End While
        Return count
    End Function

    Public Shared Function HolidayDays(fromDay As Date, toDay As Date, holidays As IEnumerable(Of HolidayItem), rules As IEnumerable(Of WeeklyOffRule)) As Integer
        If holidays Is Nothing Then Throw New ArgumentNullException(NameOf(holidays))
        Dim list = rules.ToList()
        Dim count As Integer = 0
        For Each holidayDate As Date In holidays.Select(Function(h) h.Day).Distinct()
            If holidayDate < fromDay.Date OrElse holidayDate > toDay.Date Then Continue For
            Dim rule = RuleOn(holidayDate, list)
            If rule IsNot Nothing AndAlso rule.IsOff(holidayDate.DayOfWeek) Then Continue For
            count += 1
        Next
        Return count
    End Function
End Class
