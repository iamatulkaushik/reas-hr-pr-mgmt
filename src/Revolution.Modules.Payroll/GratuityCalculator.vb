Imports Revolution.Core

''' <summary>
''' Gratuity at exit from the rule row (stat.GratuityRule, CR 0013): days of wages for each counted year, a day's wage being the last monthly wage divided by the rule's
''' month divisor. Nothing is typed here. Service runs from the joining date to the end of the exit date (the exit date counts as a day worked). Whole years decide
''' eligibility; a part year counts as a full year for the amount only when it is more than the rule's limit of months (six months and a day is more; exactly six months is not). A death or disablement waives the minimum
''' years (waiveMinimum, decided by the caller from the exit reason). Rounded once to the nearest rupee, midpoint away from zero, then capped when the rule has a cap.
''' lastMonthlyWage is Basic plus DA of the last month; the caller decides which heads count.
''' </summary>
Public NotInheritable Class GratuityCalculator
    Private Sub New()
    End Sub

    Public Shared Function Calculate(joinDate As Date, exitDate As Date, lastMonthlyWage As Decimal, isFixedTerm As Boolean,
                                     waiveMinimum As Boolean, rule As GratuityRule) As GratuityResult
        If rule Is Nothing Then Throw New ArgumentNullException(NameOf(rule))
        If exitDate < joinDate Then Throw New ArgumentOutOfRangeException(NameOf(exitDate), "The exit date is before the joining date.")
        If lastMonthlyWage < 0D Then Throw New ArgumentOutOfRangeException(NameOf(lastMonthlyWage), "The wage cannot be negative.")
        Dim serviceEnd As Date = exitDate.AddDays(1)
        Dim years As Integer = WholeYears(joinDate, serviceEnd)
        Dim months As Integer = WholeMonths(joinDate.AddYears(years), serviceEnd)
        Dim minimum As Integer = If(isFixedTerm, rule.MinYearsFixedTerm, rule.MinYearsRegular)
        If Not waiveMinimum AndAlso years < minimum Then
            Return New GratuityResult(False, $"Service is under the {minimum} years the rule needs.", years, months, 0, 0D, False)
        End If
        Dim counted As Integer = years + If(serviceEnd > joinDate.AddYears(years).AddMonths(rule.RoundUpAfterMonths), 1, 0)
        Dim amount As Decimal = Math.Round(lastMonthlyWage * rule.DaysPerYear / rule.MonthDivisor * counted, 0, MidpointRounding.AwayFromZero)
        Dim capped As Boolean = rule.CapAmount > 0D AndAlso amount > rule.CapAmount
        Return New GratuityResult(True, String.Empty, years, months, counted, If(capped, rule.CapAmount, amount), capped)
    End Function

    Private Shared Function WholeYears(startDate As Date, endExclusive As Date) As Integer
        Dim years As Integer = endExclusive.Year - startDate.Year
        If startDate.AddYears(years) > endExclusive Then years -= 1
        Return Math.Max(0, years)
    End Function

    Private Shared Function WholeMonths(startDate As Date, endExclusive As Date) As Integer
        Dim months As Integer = (endExclusive.Year - startDate.Year) * 12 + endExclusive.Month - startDate.Month
        If startDate.AddMonths(months) > endExclusive Then months -= 1
        Return Math.Max(0, months)
    End Function
End Class
