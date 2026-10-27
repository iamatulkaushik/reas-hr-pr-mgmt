Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Fixture rule: eligible up to 21,000 a month, calculated on at most 7,000 a month, 8.33% to 20%, at least 30 days worked.</summary>
Public Class BonusCalculatorTests
    Private Shared Function Rule() As BonusRule
        Return New BonusRule(New Date(2020, 4, 1), 21000D, 7000D, 8.33D, 20D, 30, True, Nothing, "test")
    End Function

    Private Shared Function Months(count As Integer, wage As Decimal) As List(Of Decimal)
        Return Enumerable.Repeat(wage, count).ToList()
    End Function

    <Fact>
    Public Sub SixMonthsAtMinimumPct_CutToCeiling()
        Dim r As BonusResult = BonusCalculator.Calculate(Rule(), 15000D, Months(6, 15000D), 180, 8.33D, 0D)
        Assert.True(r.IsEligible)
        Assert.Equal(42000D, r.WageBase)
        Assert.Equal(3499D, r.GrossBonus)
        Assert.Equal(3499D, r.Amount)
    End Sub

    <Fact>
    Public Sub MaximumPct_UsesTwentyPercent()
        Assert.Equal(8400D, BonusCalculator.Calculate(Rule(), 15000D, Months(6, 15000D), 180, 20D, 0D).Amount)
    End Sub

    <Fact>
    Public Sub WageBelowCeiling_UsesTheWage()
        Assert.Equal(30000D, BonusCalculator.Calculate(Rule(), 5000D, Months(6, 5000D), 180, 8.33D, 0D).WageBase)
    End Sub

    <Fact>
    Public Sub AlreadyPaid_IsTakenOff()
        Assert.Equal(2499D, BonusCalculator.Calculate(Rule(), 15000D, Months(6, 15000D), 180, 8.33D, 1000D).Amount)
    End Sub

    <Fact>
    Public Sub AlreadyPaidMoreThanBonus_PaysZero()
        Assert.Equal(0D, BonusCalculator.Calculate(Rule(), 15000D, Months(6, 15000D), 180, 8.33D, 9000D).Amount)
    End Sub

    <Fact>
    Public Sub WageAboveEligibilityCeiling_NotEligible()
        Dim r As BonusResult = BonusCalculator.Calculate(Rule(), 22000D, Months(6, 22000D), 180, 8.33D, 0D)
        Assert.False(r.IsEligible)
        Assert.Equal(0D, r.Amount)
    End Sub

    <Fact>
    Public Sub UnderMinimumDays_NotEligible()
        Dim r As BonusResult = BonusCalculator.Calculate(Rule(), 15000D, Months(1, 15000D), 29, 8.33D, 0D)
        Assert.False(r.IsEligible)
        Assert.Equal(0D, r.Amount)
    End Sub

    <Fact>
    Public Sub ExactlyMinimumDays_IsEligible()
        Assert.True(BonusCalculator.Calculate(Rule(), 15000D, Months(1, 15000D), 30, 8.33D, 0D).IsEligible)
    End Sub

    <Fact>
    Public Sub PctOutsideRange_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() BonusCalculator.Calculate(Rule(), 15000D, Months(6, 15000D), 180, 8D, 0D))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() BonusCalculator.Calculate(Rule(), 15000D, Months(6, 15000D), 180, 20.5D, 0D))
    End Sub

    <Fact>
    Public Sub NoCeilings_UsesFullWage()
        Dim open As New BonusRule(New Date(2020, 4, 1), 0D, 0D, 8.33D, 20D, 30, True, Nothing, "test")
        Assert.Equal(100000D, BonusCalculator.Calculate(open, 50000D, Months(2, 50000D), 60, 8.33D, 0D).WageBase)
    End Sub
End Class
