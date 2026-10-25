Imports Revolution.Core

''' <summary>Tax on an income by the slabs of one rule. Not rounded. Slab edges come from the rule rows, never from code.</summary>
Public NotInheritable Class TdsSlabTax
    Private Sub New()
    End Sub

    Public Shared Function Compute(income As Decimal, rule As TdsRule) As Decimal
        If rule Is Nothing Then
            Throw New ArgumentNullException(NameOf(rule))
        End If
        Dim tax As Decimal = 0D
        For Each slab As TdsSlab In rule.Slabs
            Dim top As Decimal = If(slab.ToAmount.HasValue, Math.Min(income, slab.ToAmount.Value), income)
            If top > slab.FromAmount Then
                tax += Rupee.Percent(top - slab.FromAmount, slab.RatePct)
            End If
        Next
        Return tax
    End Function
End Class
