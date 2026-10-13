Imports Revolution.Core

''' <summary>
''' Wage used for EPF and ESI. Under the Code on Wages rule, exclusions (such as allowances) count only up to the share the rule allows,
''' so the wage never falls below WageFloorPct of total pay. Without that rule the wage is the sum of the components flagged for the fund.
''' </summary>
Public NotInheritable Class StatutoryWage
    Private Sub New()
    End Sub

    Public Shared Function Compute(flaggedWage As Decimal, totalRemuneration As Decimal, exclusions As Decimal, rule As WageRule) As Decimal
        If rule Is Nothing Then
            Throw New ArgumentNullException(NameOf(rule))
        End If
        If flaggedWage < 0D OrElse totalRemuneration < 0D OrElse exclusions < 0D Then
            Throw New ArgumentOutOfRangeException(NameOf(totalRemuneration), "Amounts cannot be negative.")
        End If
        If Not rule.UseCodeWage Then
            Return flaggedWage
        End If
        If rule.WageFloorPct < 0D OrElse rule.WageFloorPct > 100D Then
            Throw New ArgumentOutOfRangeException(NameOf(rule), "Wage floor must be between 0 and 100.")
        End If
        If exclusions > totalRemuneration Then
            Throw New ArgumentOutOfRangeException(NameOf(exclusions), "Exclusions cannot exceed total pay.")
        End If
        Dim allowedExclusion As Decimal = Rupee.Percent(totalRemuneration, 100D - rule.WageFloorPct)
        Return totalRemuneration - Math.Min(exclusions, allowedExclusion)
    End Function
End Class
