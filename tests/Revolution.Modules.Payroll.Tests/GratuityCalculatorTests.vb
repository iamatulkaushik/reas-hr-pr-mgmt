Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Known answers by hand. Terms: 5 years regular, 1 year fixed term, 15 days over 26, round up above 6 months, cap 2,000,000. The CA signs the expected values.</summary>
Public Class GratuityCalculatorTests
    Private Shared Function Terms() As GratuityTerms
        Return New GratuityTerms(5, 1, 15D, 26D, 6, 2000000D)
    End Function

    <Fact>
    Public Sub TenYears_WholeYears()
        Dim r As GratuityResult = GratuityCalculator.Calculate(New Date(2016, 1, 1), New Date(2026, 1, 1), 26000D, False, False, Terms())
        Assert.True(r.Eligible)
        Assert.Equal(10, r.CompletedYears)
        Assert.Equal(10, r.CountedYears)
        Assert.Equal(150000D, r.Amount)
    End Sub

    <Fact>
    Public Sub PartYearAboveSixMonths_RoundsUp()
        Dim r As GratuityResult = GratuityCalculator.Calculate(New Date(2016, 1, 1), New Date(2021, 8, 15), 26000D, False, False, Terms())
        Assert.Equal(5, r.CompletedYears)
        Assert.Equal(6, r.CountedYears)
        Assert.Equal(90000D, r.Amount)
    End Sub

    <Fact>
    Public Sub PartYearExactlySixMonths_DoesNotRoundUp()
        Dim r As GratuityResult = GratuityCalculator.Calculate(New Date(2016, 1, 1), New Date(2021, 6, 30), 26000D, False, False, Terms())
        Assert.Equal(5, r.CountedYears)
        Assert.Equal(75000D, r.Amount)
    End Sub

    <Fact>
    Public Sub PartYearOneDayOverSixMonths_RoundsUp()
        Dim r As GratuityResult = GratuityCalculator.Calculate(New Date(2016, 1, 1), New Date(2021, 7, 1), 26000D, False, False, Terms())
        Assert.Equal(6, r.CountedYears)
    End Sub

    <Fact>
    Public Sub Regular_BelowMinimum_NotEligible()
        Dim r As GratuityResult = GratuityCalculator.Calculate(New Date(2022, 1, 1), New Date(2025, 12, 31), 26000D, False, False, Terms())
        Assert.False(r.Eligible)
        Assert.Equal(0D, r.Amount)
        Assert.Equal(4, r.CompletedYears)
    End Sub

    <Fact>
    Public Sub FixedTerm_UsesItsOwnMinimum()
        Dim r As GratuityResult = GratuityCalculator.Calculate(New Date(2022, 1, 1), New Date(2025, 12, 31), 26000D, True, False, Terms())
        Assert.True(r.Eligible)
        Assert.Equal(60000D, r.Amount)
    End Sub

    <Fact>
    Public Sub Death_WaivesMinimumService()
        Dim r As GratuityResult = GratuityCalculator.Calculate(New Date(2024, 1, 1), New Date(2026, 1, 1), 26000D, False, True, Terms())
        Assert.True(r.Eligible)
        Assert.Equal(2, r.CountedYears)
        Assert.Equal(30000D, r.Amount)
    End Sub

    <Fact>
    Public Sub RoundedToNearestRupee()
        Dim r As GratuityResult = GratuityCalculator.Calculate(New Date(2020, 1, 1), New Date(2025, 12, 31), 10000D, False, False, Terms())
        Assert.Equal(6, r.CountedYears)
        Assert.Equal(34615D, r.Amount)
    End Sub

    <Fact>
    Public Sub CapApplied()
        Dim r As GratuityResult = GratuityCalculator.Calculate(New Date(2006, 1, 1), New Date(2026, 1, 1), 200000D, False, False, Terms())
        Assert.True(r.CapApplied)
        Assert.Equal(2000000D, r.Amount)
    End Sub

    <Fact>
    Public Sub NoCapWhenCapIsZero()
        Dim t As New GratuityTerms(5, 1, 15D, 26D, 6, 0D)
        Dim r As GratuityResult = GratuityCalculator.Calculate(New Date(2006, 1, 1), New Date(2026, 1, 1), 200000D, False, False, t)
        Assert.False(r.CapApplied)
        Assert.Equal(2307692D, r.Amount)
    End Sub

    <Fact>
    Public Sub ExitBeforeJoin_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() GratuityCalculator.Calculate(New Date(2026, 1, 2), New Date(2026, 1, 1), 1D, False, False, Terms()))
    End Sub

    <Fact>
    Public Sub NegativeWage_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() GratuityCalculator.Calculate(New Date(2016, 1, 1), New Date(2026, 1, 1), -1D, False, False, Terms()))
    End Sub

    <Fact>
    Public Sub NullTerms_Throws()
        Assert.Throws(Of ArgumentNullException)(Function() GratuityCalculator.Calculate(New Date(2016, 1, 1), New Date(2026, 1, 1), 1D, False, False, Nothing))
    End Sub
End Class
