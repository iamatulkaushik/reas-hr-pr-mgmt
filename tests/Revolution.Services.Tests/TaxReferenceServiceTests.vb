Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

''' <summary>CR 0018: form numbers and section references are read like the other rates. A missing or unverified row is a failure, never a guess.</summary>
Public Class TaxReferenceServiceTests
    Private Shared Function Row(key As String, value As String, verified As Boolean) As TaxReference
        Return New TaxReference(key, 2026, value, "title", verified, Nothing, "test")
    End Function

    <Fact>
    Public Sub GetTaxFormNo_ReturnsTheNumber_ForTheKeyAndYear()
        Dim repository As New FakeStatutoryRateRepository() With {.TaxForm = Row("SALARY_CERT", "130", True)}
        Dim result = New StatutoryRateService(repository).GetTaxFormNo("SALARY_CERT", 2026)
        Assert.True(result.Succeeded)
        Assert.Equal("130", result.Value)
        Assert.Equal("SALARY_CERT", repository.LastKey)
        Assert.Equal(2026, repository.LastFinancialYear)
    End Sub

    <Fact>
    Public Sub GetTaxFormNo_FailsWhenThereIsNoRow()
        Dim result = New StatutoryRateService(New FakeStatutoryRateRepository()).GetTaxFormNo("SALARY_CERT", 2026)
        Assert.False(result.Succeeded)
        Assert.Contains("2026-27", result.Message)
    End Sub

    <Fact>
    Public Sub GetTaxFormNo_FailsWhenTheRowIsUnverified_AndVerificationIsRequired()
        Dim repository As New FakeStatutoryRateRepository() With {.TaxForm = Row("SALARY_CERT", "130", False)}
        Assert.False(New StatutoryRateService(repository).GetTaxFormNo("SALARY_CERT", 2026).Succeeded)
    End Sub

    <Fact>
    Public Sub GetTaxFormNo_AcceptsAnUnverifiedRow_OnlyWhenVerificationIsNotRequired()
        Dim repository As New FakeStatutoryRateRepository() With {.TaxForm = Row("SALARY_CERT", "130", False), .VerificationRequired = False}
        Assert.True(New StatutoryRateService(repository).GetTaxFormNo("SALARY_CERT", 2026).Succeeded)
    End Sub

    <Fact>
    Public Sub GetTaxFormNo_FailsWhenTheRowHasNoNumber()
        Dim repository As New FakeStatutoryRateRepository() With {.TaxForm = Row("SALARY_CERT", " ", True)}
        Assert.False(New StatutoryRateService(repository).GetTaxFormNo("SALARY_CERT", 2026).Succeeded)
    End Sub

    <Fact>
    Public Sub GetTaxSectionRef_ReturnsTheReference_ForTheKeyAndYear()
        Dim repository As New FakeStatutoryRateRepository() With {.TaxSectionRef = Row("SALARY_TDS", "392", True)}
        Dim result = New StatutoryRateService(repository).GetTaxSectionRef("SALARY_TDS", 2026)
        Assert.True(result.Succeeded)
        Assert.Equal("392", result.Value)
        Assert.Equal("SALARY_TDS", repository.LastKey)
    End Sub

    <Fact>
    Public Sub GetTaxSectionRef_FailsWhenMissing_OrUnverifiedWhileRequired()
        Assert.False(New StatutoryRateService(New FakeStatutoryRateRepository()).GetTaxSectionRef("SALARY_TDS", 2026).Succeeded)
        Dim repository As New FakeStatutoryRateRepository() With {.TaxSectionRef = Row("SALARY_TDS", "392", False)}
        Assert.False(New StatutoryRateService(repository).GetTaxSectionRef("SALARY_TDS", 2026).Succeeded)
    End Sub

    <Fact>
    Public Sub EmptyKey_Throws()
        Dim service As New StatutoryRateService(New FakeStatutoryRateRepository())
        Assert.Throws(Of ArgumentException)(Function() service.GetTaxFormNo(" ", 2026))
        Assert.Throws(Of ArgumentException)(Function() service.GetTaxSectionRef("", 2026))
    End Sub
End Class
