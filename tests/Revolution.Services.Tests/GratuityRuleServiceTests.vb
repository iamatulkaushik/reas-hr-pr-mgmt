Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

''' <summary>CR 0013: the gratuity rule is read like the other rates. A missing or unverified row is a failure, never a default.</summary>
Public Class GratuityRuleServiceTests
    Private Shared ReadOnly AsOf As New Date(2026, 10, 1)

    Private Shared Function Row(verified As Boolean) As GratuityRule
        Return New GratuityRule(New Date(2025, 11, 21), 5, 1, 15D, 26D, 6, 2000000D, verified, If(verified, New Date(2026, 1, 5), CType(Nothing, Date?)), "test")
    End Function

    <Fact>
    Public Sub GetGratuityRule_ReturnsTheVerifiedRow_WithAllItsValues()
        Dim repository As New FakeStatutoryRateRepository() With {.Gratuity = Row(True)}
        Dim result = New StatutoryRateService(repository).GetGratuityRule(AsOf)
        Assert.True(result.Succeeded)
        Assert.Equal(5, result.Value.MinYearsRegular)
        Assert.Equal(1, result.Value.MinYearsFixedTerm)
        Assert.Equal(15D, result.Value.DaysPerYear)
        Assert.Equal(26D, result.Value.MonthDivisor)
        Assert.Equal(6, result.Value.RoundUpAfterMonths)
        Assert.Equal(2000000D, result.Value.CapAmount)
        Assert.Equal(AsOf, repository.LastAsOf)
    End Sub

    <Fact>
    Public Sub GetGratuityRule_FailsOnAnUnverifiedRow_WhenVerificationIsRequired()
        Dim repository As New FakeStatutoryRateRepository() With {.Gratuity = Row(False)}
        Dim result = New StatutoryRateService(repository).GetGratuityRule(AsOf)
        Assert.False(result.Succeeded)
        Assert.NotEqual(String.Empty, result.Message)
    End Sub

    <Fact>
    Public Sub GetGratuityRule_AcceptsAnUnverifiedRow_OnlyWhenVerificationIsNotRequired()
        Dim repository As New FakeStatutoryRateRepository() With {.Gratuity = Row(False), .VerificationRequired = False}
        Assert.True(New StatutoryRateService(repository).GetGratuityRule(AsOf).Succeeded)
    End Sub

    <Fact>
    Public Sub GetGratuityRule_FailsWhenThereIsNoRow()
        Dim result = New StatutoryRateService(New FakeStatutoryRateRepository()).GetGratuityRule(AsOf)
        Assert.False(result.Succeeded)
        Assert.NotEqual(String.Empty, result.Message)
    End Sub
End Class
