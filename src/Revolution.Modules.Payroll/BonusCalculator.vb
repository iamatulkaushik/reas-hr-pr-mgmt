Imports Revolution.Core

''' <summary>
''' Bonus paid in F&amp;F for the part of the accounting year worked (Owner, 2026-10-27). Everything comes from the bonus rule row (stat.BonusRule): the wage
''' ceiling for eligibility, the ceiling each month's wage is cut to, the percentage range and the minimum days worked. The caller gives the monthly wages
''' (Basic plus DA) of each month worked in the accounting year, the days worked, the percentage chosen inside the rule's range, and any bonus already paid in the
''' year. Rounded once to the nearest rupee, midpoint away from zero. An unverified rule is the caller's concern: the rate service refuses it.
''' </summary>
Public NotInheritable Class BonusCalculator
    Private Sub New()
    End Sub

    Public Shared Function Calculate(rule As BonusRule, lastMonthlyWage As Decimal, monthlyWages As IReadOnlyList(Of Decimal), daysWorked As Integer,
                                     pct As Decimal, alreadyPaid As Decimal) As BonusResult
        If rule Is Nothing Then Throw New ArgumentNullException(NameOf(rule))
        If monthlyWages Is Nothing Then Throw New ArgumentNullException(NameOf(monthlyWages))
        If lastMonthlyWage < 0D OrElse alreadyPaid < 0D Then Throw New ArgumentOutOfRangeException(NameOf(lastMonthlyWage), "Money cannot be negative.")
        If daysWorked < 0 Then Throw New ArgumentOutOfRangeException(NameOf(daysWorked), "Days cannot be negative.")
        If pct < rule.MinPct OrElse pct > rule.MaxPct Then
            Throw New ArgumentOutOfRangeException(NameOf(pct), $"The bonus must be between {rule.MinPct}% and {rule.MaxPct}%.")
        End If
        If Not rule.IsEligibleOnWage(lastMonthlyWage) Then Return NotEligible("The wage is above the bonus eligibility ceiling.", pct, alreadyPaid)
        If daysWorked < rule.MinDaysWorked Then Return NotEligible($"Worked {daysWorked} days, the rule needs {rule.MinDaysWorked}.", pct, alreadyPaid)
        Dim wageBase As Decimal = 0D
        For Each wage As Decimal In monthlyWages
            If wage < 0D Then Throw New ArgumentOutOfRangeException(NameOf(monthlyWages), "A monthly wage cannot be negative.")
            wageBase += rule.CalculationWage(wage)
        Next
        Dim gross As Decimal = Rupee.Round(wageBase * pct / 100D, RateRounding.Nearest)
        Return New BonusResult(True, String.Empty, wageBase, pct, gross, alreadyPaid, Math.Max(0D, gross - alreadyPaid))
    End Function

    Private Shared Function NotEligible(reason As String, pct As Decimal, alreadyPaid As Decimal) As BonusResult
        Return New BonusResult(False, reason, 0D, pct, 0D, alreadyPaid, 0D)
    End Function
End Class
