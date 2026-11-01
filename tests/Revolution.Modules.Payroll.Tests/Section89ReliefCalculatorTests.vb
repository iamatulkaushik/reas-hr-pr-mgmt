Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Known answers for section 89 relief with a simple test slab (5% above 3,00,000, 20% above 6,00,000, no cess). The slab is a fixture; the real tax comes from the TDS rules.</summary>
Public Class Section89ReliefCalculatorTests
    Private Shared Function Slab(income As Decimal) As Decimal
        Dim tax As Decimal = 0D
        If income > 300000D Then tax += (Math.Min(income, 600000D) - 300000D) * 0.05D
        If income > 600000D Then tax += (income - 600000D) * 0.2D
        Return tax
    End Function

    Private Shared Function Year(income As Decimal, arrears As Decimal) As Section89Year
        Return New Section89Year("2025-26", income, arrears, AddressOf Slab)
    End Function

    <Fact>
    Public Sub ArrearsLiftThisYearIntoAHigherSlab_ReliefIsTheDifference()
        ' This year 5,50,000 + 1,00,000 arrears crosses 6,00,000: extra tax 5,000 + 10,000 = 15,000... on 50,000 at 5% and 50,000 at 20% = 2,500 + 10,000.
        ' Own year 3,50,000 + 1,00,000 stays under 6,00,000: extra tax 5,000.
        Dim r As Section89Result = Section89ReliefCalculator.Calculate(550000D, AddressOf Slab, New List(Of Section89Year) From {Year(350000D, 100000D)})
        Assert.Equal(12500D, r.TaxOnArrearsThisYear)
        Assert.Equal(5000D, r.TaxOnArrearsInOwnYears)
        Assert.Equal(7500D, r.Relief)
    End Sub

    <Fact>
    Public Sub NoGainFromSpreading_ReliefIsZero()
        Dim r As Section89Result = Section89ReliefCalculator.Calculate(200000D, AddressOf Slab, New List(Of Section89Year) From {Year(350000D, 50000D)})
        Assert.Equal(0D, r.TaxOnArrearsThisYear)
        Assert.Equal(2500D, r.TaxOnArrearsInOwnYears)
        Assert.Equal(0D, r.Relief)
    End Sub

    <Fact>
    Public Sub SeveralYears_TheirTaxesAreAdded()
        Dim r As Section89Result = Section89ReliefCalculator.Calculate(550000D, AddressOf Slab, New List(Of Section89Year) From {Year(350000D, 40000D), Year(320000D, 30000D)})
        Assert.Equal(2, r.YearTaxes.Count)
        Assert.Equal(2000D, r.YearTaxes(0))
        Assert.Equal(1500D, r.YearTaxes(1))
        Assert.Equal(3500D, r.TaxOnArrearsInOwnYears)
        Assert.Equal(70000D * 0D + (50000D * 0.05D + 20000D * 0.2D), r.TaxOnArrearsThisYear)
    End Sub

    <Fact>
    Public Sub NoYears_NoRelief()
        Assert.Equal(0D, Section89ReliefCalculator.Calculate(550000D, AddressOf Slab, New List(Of Section89Year)()).Relief)
    End Sub

    <Fact>
    Public Sub NegativeInputs_Throw()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New Section89Year("x", -1D, 0D, AddressOf Slab))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New Section89Year("x", 0D, -1D, AddressOf Slab))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() Section89ReliefCalculator.Calculate(-1D, AddressOf Slab, New List(Of Section89Year)()))
    End Sub
End Class
