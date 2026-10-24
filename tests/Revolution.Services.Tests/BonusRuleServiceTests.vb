Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

''' <summary>CR 0012 #3: the bonus rule is read like the other rates. A missing or unverified row is a failure, never a default.</summary>
Public Class BonusRuleServiceTests
    Private Shared ReadOnly AsOf As New Date(2026, 10, 1)

    Private Shared Function Row(verified As Boolean, Optional eligibility As Decimal = 21000D, Optional calc As Decimal = 7000D) As BonusRule
        Return New BonusRule(New Date(2026, 4, 1), eligibility, calc, 8.33D, 20D, 30, verified,
                             If(verified, New Date(2026, 4, 2), CType(Nothing, Date?)), "test")
    End Function

    <Fact>
    Public Sub GetBonusRule_ReturnsTheVerifiedRow_WithAllItsValues()
        Dim repository As New FakeStatutoryRateRepository() With {.Bonus = Row(True)}
        Dim result = New StatutoryRateService(repository).GetBonusRule(AsOf)
        Assert.True(result.Succeeded)
        Assert.Equal(21000D, result.Value.EligibilityWageCeiling)
        Assert.Equal(7000D, result.Value.CalcWageCeiling)
        Assert.Equal(8.33D, result.Value.MinPct)
        Assert.Equal(20D, result.Value.MaxPct)
        Assert.Equal(30, result.Value.MinDaysWorked)
        Assert.Equal(AsOf, repository.LastAsOf)
    End Sub

    <Fact>
    Public Sub GetBonusRule_FailsOnAnUnverifiedRow_WhenVerificationIsRequired()
        Dim repository As New FakeStatutoryRateRepository() With {.Bonus = Row(False)}
        Dim result = New StatutoryRateService(repository).GetBonusRule(AsOf)
        Assert.False(result.Succeeded)
        Assert.NotEqual(String.Empty, result.Message)
    End Sub

    <Fact>
    Public Sub GetBonusRule_AcceptsAnUnverifiedRow_OnlyWhenVerificationIsNotRequired()
        Dim repository As New FakeStatutoryRateRepository() With {.Bonus = Row(False), .VerificationRequired = False}
        Assert.True(New StatutoryRateService(repository).GetBonusRule(AsOf).Succeeded)
    End Sub

    <Fact>
    Public Sub GetBonusRule_FailsWhenThereIsNoRow()
        Dim result = New StatutoryRateService(New FakeStatutoryRateRepository()).GetBonusRule(AsOf)
        Assert.False(result.Succeeded)
        Assert.NotEqual(String.Empty, result.Message)
    End Sub

    <Fact>
    Public Sub IsEligibleOnWage_IncludesTheCeiling_AndExcludesAboveIt()
        Dim rule = Row(True)
        Assert.True(rule.IsEligibleOnWage(21000D))
        Assert.False(rule.IsEligibleOnWage(21000.01D))
        Assert.True(rule.IsEligibleOnWage(0D))
    End Sub

    <Fact>
    Public Sub CalculationWage_IsCappedAtTheCalculationCeiling_ButNeverRaised()
        Dim rule = Row(True)
        Assert.Equal(7000D, rule.CalculationWage(15000D))
        Assert.Equal(7000D, rule.CalculationWage(7000D))
        Assert.Equal(5000D, rule.CalculationWage(5000D))
    End Sub

    <Fact>
    Public Sub AZeroCeiling_MeansNoCeiling()
        Dim rule = Row(True, 0D, 0D)
        Assert.True(rule.IsEligibleOnWage(1000000D))
        Assert.Equal(1000000D, rule.CalculationWage(1000000D))
    End Sub
End Class
