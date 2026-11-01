''' <summary>Section 89 relief, worked as on Form 10E. TaxOnArrearsThisYear (A) less TaxOnArrearsInOwnYears (B). Relief is never negative.</summary>
Public NotInheritable Class Section89Result
    Public Sub New(taxOnArrearsThisYear As Decimal, yearTaxes As IReadOnlyList(Of Decimal))
        Me.TaxOnArrearsThisYear = taxOnArrearsThisYear
        Me.YearTaxes = yearTaxes
    End Sub

    ''' <summary>A: tax on this year's income with the arrears, less tax on this year's income without them.</summary>
    Public ReadOnly Property TaxOnArrearsThisYear As Decimal
    ''' <summary>Extra tax on the arrears had each been taxed in its own year, in the order the years were given.</summary>
    Public ReadOnly Property YearTaxes As IReadOnlyList(Of Decimal)

    ''' <summary>B: the total of YearTaxes.</summary>
    Public ReadOnly Property TaxOnArrearsInOwnYears As Decimal
        Get
            Return YearTaxes.Sum()
        End Get
    End Property

    ''' <summary>Tax relief in whole rupees. Zero when taxing the arrears this year costs no more than taxing them in their own years.</summary>
    Public ReadOnly Property Relief As Decimal
        Get
            Return Math.Max(0D, TaxOnArrearsThisYear - TaxOnArrearsInOwnYears)
        End Get
    End Property
End Class
