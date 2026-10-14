Imports Revolution.Core

''' <summary>
''' Gratuity on exit: last drawn monthly wage (basic and DA) times days per year over the month divisor (15 over 26) times counted years.
''' Service runs from the join date to the exit date, both days included. Counted years are completed years, plus one when the part year is
''' in excess of the round-up months. Minimum service must be met unless it is waived (death or disablement), whatever the CA confirms.
''' All values come from the rule row. The cap is applied last. Nearest rupee.
''' </summary>
Public NotInheritable Class GratuityCalculator
    Private Sub New()
    End Sub

    Public Shared Function Calculate(joinDate As Date, exitDate As Date, lastDrawnMonthlyWage As Decimal, isFixedTerm As Boolean, waivesMinimumService As Boolean, terms As GratuityTerms) As GratuityResult
        If terms Is Nothing Then Throw New ArgumentNullException(NameOf(terms))
        If exitDate < joinDate Then Throw New ArgumentOutOfRangeException(NameOf(exitDate), "Exit cannot be before joining.")
        If lastDrawnMonthlyWage < 0D Then Throw New ArgumentOutOfRangeException(NameOf(lastDrawnMonthlyWage), "Wage cannot be negative.")
        If terms.MonthDivisor <= 0D OrElse terms.DaysPerYear <= 0D Then Throw New ArgumentOutOfRangeException(NameOf(terms), "Rule values must be above zero.")

        Dim endDay As Date = exitDate.AddDays(1)
        Dim years As Integer = 0
        While joinDate.AddYears(years + 1) <= endDay
            years += 1
        End While
        Dim counted As Integer = years
        If endDay > joinDate.AddYears(years).AddMonths(terms.RoundUpAfterMonths) Then counted += 1

        Dim minYears As Integer = If(isFixedTerm, terms.MinYearsFixedTerm, terms.MinYearsRegular)
        If Not waivesMinimumService AndAlso years < minYears Then
            Return New GratuityResult(False, years, counted, 0D, False, "Service is below the minimum of " & minYears.ToString(Globalization.CultureInfo.InvariantCulture) & " years.")
        End If

        Dim amount As Decimal = Rupee.Round(lastDrawnMonthlyWage * terms.DaysPerYear * counted / terms.MonthDivisor, RateRounding.Nearest)
        Dim capped As Boolean = terms.CapAmount > 0D AndAlso amount > terms.CapAmount
        Return New GratuityResult(True, years, counted, If(capped, terms.CapAmount, amount), capped, Nothing)
    End Function
End Class
