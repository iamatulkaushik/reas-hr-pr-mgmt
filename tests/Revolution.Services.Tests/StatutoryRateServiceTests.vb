Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class StatutoryRateServiceTests
    Private Shared ReadOnly AsOf As New Date(2026, 4, 30)

    <Fact>
    Public Sub Epf_MissingRow_Fails_WithTheDate()
        Dim result = New StatutoryRateService(New FakeStatutoryRateRepository()).GetEpfRate(AsOf)
        Assert.False(result.Succeeded)
        Assert.Contains("No EPF rate is set for 30-04-2026", result.Message)
    End Sub

    <Fact>
    Public Sub Epf_UnverifiedRow_Fails_WhenVerificationIsRequired()
        Dim repo As New FakeStatutoryRateRepository() With {.Epf = FakeStatutoryRateRepository.EpfRow(False)}
        Dim result = New StatutoryRateService(repo).GetEpfRate(AsOf)
        Assert.False(result.Succeeded)
        Assert.Contains("has not been verified by the CA", result.Message)
        Assert.Contains("01-04-2025", result.Message)
    End Sub

    <Fact>
    Public Sub Epf_UnverifiedRow_Passes_WhenVerificationIsOff()
        Dim repo As New FakeStatutoryRateRepository() With {.Epf = FakeStatutoryRateRepository.EpfRow(False), .VerificationRequired = False}
        Dim result = New StatutoryRateService(repo).GetEpfRate(AsOf)
        Assert.True(result.Succeeded)
        Assert.Equal(12D, result.Value.EmployeePct)
    End Sub

    <Fact>
    Public Sub Epf_VerifiedRow_Passes()
        Dim repo As New FakeStatutoryRateRepository() With {.Epf = FakeStatutoryRateRepository.EpfRow(True)}
        Dim result = New StatutoryRateService(repo).GetEpfRate(AsOf)
        Assert.True(result.Succeeded)
        Assert.Equal(15000D, result.Value.WageCeiling)
    End Sub

    <Fact>
    Public Sub TheDateAsked_IsPassedToTheRepository()
        Dim repo As New FakeStatutoryRateRepository() With {.Esi = New EsiRate(New Date(2025, 4, 1), 0.75D, 3.25D, 21000D, 0D, RateRounding.Up, True, "t")}
        Dim result = New StatutoryRateService(repo).GetEsiRate(AsOf)
        Assert.True(result.Succeeded)
        Assert.Equal(AsOf, repo.LastAsOf)
        Assert.Equal(RateRounding.Up, result.Value.Rounding)
    End Sub

    <Fact>
    Public Sub Esi_MissingRow_Fails()
        Assert.False(New StatutoryRateService(New FakeStatutoryRateRepository()).GetEsiRate(AsOf).Succeeded)
    End Sub

    <Fact>
    Public Sub Pt_MissingRule_Fails_NamingTheState()
        Dim repo As New FakeStatutoryRateRepository()
        Dim result = New StatutoryRateService(repo).GetPtRule(27, AsOf)
        Assert.False(result.Succeeded)
        Assert.Contains("state 27", result.Message)
        Assert.Equal(27, repo.LastStateId)
    End Sub

    <Fact>
    Public Sub Pt_RuleWithoutSlabs_Fails()
        Dim repo As New FakeStatutoryRateRepository() With {.Pt = FakeStatutoryRateRepository.PtRow(True, 0)}
        Dim result = New StatutoryRateService(repo).GetPtRule(27, AsOf)
        Assert.False(result.Succeeded)
        Assert.Contains("has no slabs", result.Message)
    End Sub

    <Fact>
    Public Sub Pt_VerifiedRuleWithSlabs_Passes()
        Dim repo As New FakeStatutoryRateRepository() With {.Pt = FakeStatutoryRateRepository.PtRow(True, 3)}
        Dim result = New StatutoryRateService(repo).GetPtRule(27, AsOf)
        Assert.True(result.Succeeded)
        Assert.Equal(3, result.Value.Slabs.Count)
    End Sub

    <Fact>
    Public Sub Pt_UnverifiedRule_Fails()
        Dim repo As New FakeStatutoryRateRepository() With {.Pt = FakeStatutoryRateRepository.PtRow(False, 3)}
        Assert.False(New StatutoryRateService(repo).GetPtRule(27, AsOf).Succeeded)
    End Sub

    <Fact>
    Public Sub WageRule_VerifiedRow_Passes_UnverifiedFails()
        Dim ok As New FakeStatutoryRateRepository() With {.Wage = New WageRule(New Date(2025, 11, 21), True, 50D, "n", True, "t")}
        Dim bad As New FakeStatutoryRateRepository() With {.Wage = New WageRule(New Date(2025, 11, 21), True, 50D, "n", False, "t")}
        Assert.True(New StatutoryRateService(ok).GetWageRule(AsOf).Succeeded)
        Assert.False(New StatutoryRateService(bad).GetWageRule(AsOf).Succeeded)
    End Sub

    <Fact>
    Public Sub NoRepository_Throws()
        Assert.Throws(Of ArgumentNullException)(Function() New StatutoryRateService(Nothing))
    End Sub
End Class
