Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

''' <summary>CR 0012 #1: deduction limits are read for the financial year and regime. None, or an unsigned row, is a failure, never a default.</summary>
Public Class TdsDeductionLimitServiceTests
    Private Shared ReadOnly AsOf As New Date(2026, 10, 1)

    Private Shared Function Limit(section As String, subKey As String, amount As Decimal, verified As Boolean) As TdsDeductionLimit
        Return New TdsDeductionLimit(2026, TdsRegime.Old, section, subKey, amount, String.Empty, verified, If(verified, New Date(2026, 4, 2), CType(Nothing, Date?)), "test")
    End Function

    Private Shared Function RepoWith(ParamArray rows As TdsDeductionLimit()) As FakeStatutoryRateRepository
        Return New FakeStatutoryRateRepository() With {.DeductionLimits = rows}
    End Function

    <Fact>
    Public Sub GetTdsDeductionLimits_ReturnsTheRows_ForTheFinancialYearAndRegimeOfTheDate()
        Dim repository = RepoWith(Limit("80C", "", 150000D, True), Limit("80D", "self", 25000D, True))
        Dim result = New StatutoryRateService(repository).GetTdsDeductionLimits(AsOf, TdsRegime.Old)
        Assert.True(result.Succeeded)
        Assert.Equal(2, result.Value.Count)
        Assert.Equal(2026, repository.LastFinancialYear)
        Assert.Equal(TdsRegime.Old, repository.LastRegime)
    End Sub

    <Fact>
    Public Sub GetTdsDeductionLimits_UsesTheStartYearOfTheFinancialYear_ForADateInJanuaryToMarch()
        Dim repository = RepoWith(Limit("80C", "", 150000D, True))
        Dim result = New StatutoryRateService(repository).GetTdsDeductionLimits(New Date(2027, 2, 15), TdsRegime.Old)
        Assert.True(result.Succeeded)
        Assert.Equal(2026, repository.LastFinancialYear)
    End Sub

    <Fact>
    Public Sub GetTdsDeductionLimits_Fails_WhenThereAreNoRows_ForTheOldRegime()
        Dim result = New StatutoryRateService(RepoWith()).GetTdsDeductionLimits(AsOf, TdsRegime.Old)
        Assert.False(result.Succeeded)
        Assert.NotEqual(String.Empty, result.Message)
    End Sub

    <Fact>
    Public Sub GetTdsDeductionLimits_ReturnsAnEmptyList_WhenThereAreNoRows_ForTheNewRegime()
        Dim result = New StatutoryRateService(RepoWith()).GetTdsDeductionLimits(AsOf, TdsRegime.New)
        Assert.True(result.Succeeded)
        Assert.Empty(result.Value)
    End Sub

    <Fact>
    Public Sub GetTdsDeductionLimits_Fails_WhenAnyRowIsNotVerified_AndSaysHowMany()
        Dim repository = RepoWith(Limit("80C", "", 150000D, True), Limit("80D", "self", 25000D, False), Limit("80D", "parent", 50000D, False))
        Dim result = New StatutoryRateService(repository).GetTdsDeductionLimits(AsOf, TdsRegime.Old)
        Assert.False(result.Succeeded)
        Assert.Contains("2 of the 3", result.Message)
    End Sub

    <Fact>
    Public Sub GetTdsDeductionLimits_AcceptsUnverifiedRows_OnlyWhenVerificationIsNotRequired()
        Dim repository = RepoWith(Limit("80C", "", 150000D, False))
        repository.VerificationRequired = False
        Assert.True(New StatutoryRateService(repository).GetTdsDeductionLimits(AsOf, TdsRegime.Old).Succeeded)
    End Sub
End Class
