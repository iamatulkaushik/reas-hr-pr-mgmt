Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

''' <summary>CR 0018 addendum: single tax rule numbers (rent share, landlord PAN threshold) are read by key for the financial year of the date. None, or an unsigned row, is a failure.</summary>
Public Class TdsRuleValueServiceTests
    Private Shared ReadOnly AsOf As New Date(2026, 10, 1)

    Private Shared Function Row(key As String, amount As Decimal, verified As Boolean) As TdsRuleValue
        Return New TdsRuleValue(key, 2020, amount, String.Empty, verified, If(verified, New Date(2026, 10, 24), CType(Nothing, Date?)), "test")
    End Function

    <Fact>
    Public Sub GetTdsRuleValue_ReturnsTheAmount_ForTheKeyAndFinancialYearOfTheDate()
        Dim repository As New FakeStatutoryRateRepository() With {.RuleValue = Row("HRA_RENT_SHARE_PCT", 10D, True)}
        Dim result = New StatutoryRateService(repository).GetTdsRuleValue("HRA_RENT_SHARE_PCT", AsOf)
        Assert.True(result.Succeeded)
        Assert.Equal(10D, result.Value)
        Assert.Equal("HRA_RENT_SHARE_PCT", repository.LastKey)
        Assert.Equal(2026, repository.LastFinancialYear)
    End Sub

    <Fact>
    Public Sub GetTdsRuleValue_UsesTheStartYearOfTheFinancialYear_ForADateInJanuaryToMarch()
        Dim repository As New FakeStatutoryRateRepository() With {.RuleValue = Row("LANDLORD_PAN_RENT", 100000D, True)}
        Dim result = New StatutoryRateService(repository).GetTdsRuleValue("LANDLORD_PAN_RENT", New Date(2027, 2, 15))
        Assert.True(result.Succeeded)
        Assert.Equal(2026, repository.LastFinancialYear)
    End Sub

    <Fact>
    Public Sub GetTdsRuleValue_Fails_WhenThereIsNoRow()
        Dim result = New StatutoryRateService(New FakeStatutoryRateRepository()).GetTdsRuleValue("HRA_RENT_SHARE_PCT", AsOf)
        Assert.False(result.Succeeded)
        Assert.Contains("HRA_RENT_SHARE_PCT", result.Message)
    End Sub

    <Fact>
    Public Sub GetTdsRuleValue_Fails_WhenTheRowIsUnverified_AndVerificationIsRequired()
        Dim repository As New FakeStatutoryRateRepository() With {.RuleValue = Row("HRA_RENT_SHARE_PCT", 10D, False)}
        Assert.False(New StatutoryRateService(repository).GetTdsRuleValue("HRA_RENT_SHARE_PCT", AsOf).Succeeded)
    End Sub

    <Fact>
    Public Sub GetTdsRuleValue_AcceptsAnUnverifiedRow_OnlyWhenVerificationIsNotRequired()
        Dim repository As New FakeStatutoryRateRepository() With {.RuleValue = Row("HRA_RENT_SHARE_PCT", 10D, False), .VerificationRequired = False}
        Assert.True(New StatutoryRateService(repository).GetTdsRuleValue("HRA_RENT_SHARE_PCT", AsOf).Succeeded)
    End Sub

    <Fact>
    Public Sub GetTdsRuleValue_WithAnEmptyKey_Throws()
        Dim service As New StatutoryRateService(New FakeStatutoryRateRepository())
        Assert.Throws(Of ArgumentException)(Function() service.GetTdsRuleValue(" ", AsOf))
    End Sub
End Class
