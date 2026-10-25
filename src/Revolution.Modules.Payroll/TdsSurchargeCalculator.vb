Imports Revolution.Core

''' <summary>
''' Surcharge on tax by total income, with marginal relief: tax plus surcharge cannot be more than the tax and surcharge at the band's
''' starting income plus the income above it. No bands means no surcharge. Not rounded.
''' </summary>
Public NotInheritable Class TdsSurchargeCalculator
    Private Sub New()
    End Sub

    Public Shared Function Compute(income As Decimal, taxAfterRebate As Decimal, rule As TdsRule) As Decimal
        If rule Is Nothing Then
            Throw New ArgumentNullException(NameOf(rule))
        End If
        Dim band As TdsSurchargeBand = BandFor(income, rule)
        If band Is Nothing OrElse taxAfterRebate <= 0D Then
            Return 0D
        End If
        Dim full As Decimal = Rupee.Percent(taxAfterRebate, band.RatePct)
        Dim taxAtEdge As Decimal = TdsSlabTax.Compute(band.FromIncome, rule)
        Dim surchargeAtEdge As Decimal = Rupee.Percent(taxAtEdge, PriorRate(band, rule))
        Dim ceiling As Decimal = taxAtEdge + surchargeAtEdge + (income - band.FromIncome)
        If taxAfterRebate + full > ceiling Then
            Return Math.Max(0D, ceiling - taxAfterRebate)
        End If
        Return full
    End Function

    Private Shared Function BandFor(income As Decimal, rule As TdsRule) As TdsSurchargeBand
        For Each band As TdsSurchargeBand In rule.Surcharges
            If income > band.FromIncome AndAlso (Not band.ToIncome.HasValue OrElse income <= band.ToIncome.Value) Then
                Return band
            End If
        Next
        Return Nothing
    End Function

    Private Shared Function PriorRate(band As TdsSurchargeBand, rule As TdsRule) As Decimal
        For Each other As TdsSurchargeBand In rule.Surcharges
            If other.ToIncome.HasValue AndAlso other.ToIncome.Value = band.FromIncome Then
                Return other.RatePct
            End If
        Next
        Return 0D
    End Function
End Class
