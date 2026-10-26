Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Known answers worked by hand on a fixture rule: 5 years regular, 1 year fixed term, 15 days, divisor 26, part year over 6 months, cap 20,00,000.</summary>
Public Class GratuityCalculatorTests
    Private Shared Function Rule() As GratuityRule
        Return New GratuityRule(New Date(2020, 4, 1), 5, 1, 15D, 26D, 6, 2000000D, True, Nothing, "test")
    End Function

    Private Shared Function Calc(joined As Date, left As Date, wage As Decimal, Optional fixedTerm As Boolean = False, Optional waive As Boolean = False) As GratuityResult
        Return GratuityCalculator.Calculate(joined, left, wage, fixedTerm, waive, Rule())
    End Function

    <Fact>
    Public Sub TenFullYears_FifteenDaysEach()
        Dim r As GratuityResult = Calc(New Date(2015, 4, 1), New Date(2025, 3, 31), 30000D)
        Assert.True(r.IsEligible)
        Assert.Equal(10, r.CompletedYears)
        Assert.Equal(0, r.ExtraMonths)
        Assert.Equal(10, r.CountedYears)
        Assert.Equal(173077D, r.Amount)
    End Sub

    <Fact>
    Public Sub PartYearOverSixMonths_CountsAsAFullYear()
        Dim r As GratuityResult = Calc(New Date(2015, 4, 1), New Date(2025, 10, 15), 30000D)
        Assert.Equal(10, r.CompletedYears)
        Assert.Equal(6, r.ExtraMonths)
        Assert.Equal(11, r.CountedYears)
    End Sub

    <Fact>
    Public Sub PartYearOfExactlySixMonths_DoesNotRoundUp()
        Dim r As GratuityResult = Calc(New Date(2015, 4, 1), New Date(2025, 9, 30), 30000D)
        Assert.Equal(6, r.ExtraMonths)
        Assert.Equal(10, r.CountedYears)
    End Sub

    <Fact>
    Public Sub PartYearOverSixMonthsAndDays_RoundsUp()
        Dim r As GratuityResult = Calc(New Date(2015, 4, 1), New Date(2025, 10, 1), 30000D)
        Assert.Equal(6, r.ExtraMonths)
        Assert.Equal(11, r.CountedYears)
        Dim r2 As GratuityResult = Calc(New Date(2015, 4, 1), New Date(2025, 11, 1), 30000D)
        Assert.Equal(7, r2.ExtraMonths)
        Assert.Equal(11, r2.CountedYears)
    End Sub

    <Fact>
    Public Sub UnderMinimumYears_NotEligible()
        Dim r As GratuityResult = Calc(New Date(2020, 5, 1), New Date(2025, 3, 31), 30000D)
        Assert.False(r.IsEligible)
        Assert.Equal(4, r.CompletedYears)
        Assert.Equal(0D, r.Amount)
        Assert.Contains("5 years", r.Reason)
    End Sub

    <Fact>
    Public Sub ExactlyMinimumYears_Eligible()
        Dim r As GratuityResult = Calc(New Date(2020, 4, 1), New Date(2025, 3, 31), 26000D)
        Assert.True(r.IsEligible)
        Assert.Equal(5, r.CountedYears)
        Assert.Equal(75000D, r.Amount)
    End Sub

    <Fact>
    Public Sub DeathOrDisablement_WaivesTheMinimum()
        Dim r As GratuityResult = Calc(New Date(2023, 4, 1), New Date(2025, 3, 31), 26000D, waive:=True)
        Assert.True(r.IsEligible)
        Assert.Equal(2, r.CountedYears)
        Assert.Equal(30000D, r.Amount)
    End Sub

    <Fact>
    Public Sub FixedTerm_UsesItsOwnMinimum()
        Dim r As GratuityResult = Calc(New Date(2024, 4, 1), New Date(2025, 3, 31), 26000D, fixedTerm:=True)
        Assert.True(r.IsEligible)
        Assert.Equal(1, r.CountedYears)
        Assert.False(Calc(New Date(2024, 4, 2), New Date(2025, 3, 31), 26000D, fixedTerm:=True).IsEligible)
    End Sub

    <Fact>
    Public Sub Amount_AboveTheCap_IsCapped()
        Dim r As GratuityResult = Calc(New Date(1985, 4, 1), New Date(2025, 3, 31), 200000D)
        Assert.True(r.WasCapped)
        Assert.Equal(2000000D, r.Amount)
    End Sub

    <Fact>
    Public Sub NoCap_WhenTheRuleCapIsZero()
        Dim noCap As New GratuityRule(New Date(2020, 4, 1), 5, 1, 15D, 26D, 6, 0D, True, Nothing, "test")
        Dim r As GratuityResult = GratuityCalculator.Calculate(New Date(1985, 4, 1), New Date(2025, 3, 31), 200000D, False, False, noCap)
        Assert.False(r.WasCapped)
        Assert.Equal(Math.Round(200000D * 15D / 26D * 40D, 0, MidpointRounding.AwayFromZero), r.Amount)
    End Sub

    <Fact>
    Public Sub JoinedOnLeapDay_Handled()
        Dim r As GratuityResult = Calc(New Date(2016, 2, 29), New Date(2025, 2, 28), 30000D)
        Assert.Equal(9, r.CompletedYears)
        Assert.Equal(0, r.ExtraMonths)
    End Sub

    <Fact>
    Public Sub ExitOnTheJoiningDay_IsOneDayOfService()
        Dim r As GratuityResult = Calc(New Date(2025, 3, 31), New Date(2025, 3, 31), 30000D)
        Assert.False(r.IsEligible)
        Assert.Equal(0, r.CompletedYears)
    End Sub

    <Fact>
    Public Sub ExitBeforeJoin_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Sub() Calc(New Date(2025, 4, 1), New Date(2025, 3, 31), 30000D))
    End Sub

    <Fact>
    Public Sub NegativeWage_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Sub() Calc(New Date(2015, 4, 1), New Date(2025, 3, 31), -1D))
    End Sub

    <Fact>
    Public Sub NoRule_Throws()
        Assert.Throws(Of ArgumentNullException)(Sub() GratuityCalculator.Calculate(New Date(2015, 4, 1), New Date(2025, 3, 31), 1D, False, False, Nothing))
    End Sub
End Class
