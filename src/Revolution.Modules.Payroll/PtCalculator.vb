Imports Revolution.Core

''' <summary>
''' Professional tax for one month from the state's rule and slabs.
''' A monthly rule is due every month. A half-yearly or yearly rule is due only in the months the rule lists (PtRule.IsDue, CR 0011):
''' in any other month the tax is zero, and in a due month it is the slab amount (the slab amount is per due month).
''' The special month amount, when the slab has one, replaces the slab amount in that month.
''' A slab applies from its FromAmount up to its ToAmount, both included.
''' </summary>
Public NotInheritable Class PtCalculator
    Private Sub New()
    End Sub

    Public Shared Function Calculate(rule As PtRule, genderId As Integer, monthlyGross As Decimal, month As Integer) As OperationResult(Of Decimal)
        If rule Is Nothing Then
            Throw New ArgumentNullException(NameOf(rule))
        End If
        If monthlyGross < 0D Then
            Throw New ArgumentOutOfRangeException(NameOf(monthlyGross), "Wage cannot be negative.")
        End If
        If month < 1 OrElse month > 12 Then
            Throw New ArgumentOutOfRangeException(NameOf(month))
        End If
        If Not rule.IsDue(month) Then
            Return OperationResult(Of Decimal).Ok(0D)
        End If
        Dim slab As PtSlab = FindSlab(rule, genderId, monthlyGross)
        If slab Is Nothing Then
            Return OperationResult(Of Decimal).Fail("No professional tax slab covers this wage.")
        End If
        Dim useSpecial As Boolean = rule.SpecialMonth.HasValue AndAlso rule.SpecialMonth.Value = month AndAlso slab.SpecialMonthTax.HasValue
        Return OperationResult(Of Decimal).Ok(If(useSpecial, slab.SpecialMonthTax.Value, slab.TaxAmount))
    End Function

    Private Shared Function FindSlab(rule As PtRule, genderId As Integer, gross As Decimal) As PtSlab
        Dim own As List(Of PtSlab) = rule.Slabs.Where(Function(s) s.GenderId = genderId).ToList()
        Dim pool As IEnumerable(Of PtSlab) = If(own.Count > 0, own, rule.Slabs.Where(Function(s) s.GenderId = 0))
        Dim best As PtSlab = pool.Where(Function(s) s.FromAmount <= gross).OrderByDescending(Function(s) s.FromAmount).FirstOrDefault()
        If best Is Nothing Then
            Return Nothing
        End If
        If best.ToAmount.HasValue AndAlso gross > best.ToAmount.Value Then
            Return Nothing
        End If
        Return best
    End Function
End Class
