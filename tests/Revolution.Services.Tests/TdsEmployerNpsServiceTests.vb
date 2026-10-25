Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

''' <summary>CR 0018: the employer NPS percent is read for the financial year and regime of the date. None, or an unsigned row, is a failure, never a default.</summary>
Public Class TdsEmployerNpsServiceTests
    Private Shared ReadOnly AsOf As New Date(2026, 10, 1)

    Private Shared Function Row(regime As TdsRegime, pct As Decimal, verified As Boolean) As TdsEmployerNps
        Return New TdsEmployerNps(2026, regime, pct, String.Empty, verified, If(verified, New Date(2026, 10, 24), CType(Nothing, Date?)), "test")
    End Function

    <Fact>
    Public Sub GetTdsEmployerNps_ReturnsThePercent_ForTheFinancialYearAndRegimeOfTheDate()
        Dim repository As New FakeStatutoryRateRepository() With {.EmployerNps = Row(TdsRegime.New, 14D, True)}
        Dim result = New StatutoryRateService(repository).GetTdsEmployerNps(AsOf, TdsRegime.New)
        Assert.True(result.Succeeded)
        Assert.Equal(14D, result.Value)
        Assert.Equal(2026, repository.LastFinancialYear)
        Assert.Equal(TdsRegime.New, repository.LastRegime)
    End Sub

    <Fact>
    Public Sub GetTdsEmployerNps_UsesTheStartYearOfTheFinancialYear_ForADateInJanuaryToMarch()
        Dim repository As New FakeStatutoryRateRepository() With {.EmployerNps = Row(TdsRegime.Old, 10D, True)}
        Dim result = New StatutoryRateService(repository).GetTdsEmployerNps(New Date(2027, 2, 15), TdsRegime.Old)
        Assert.True(result.Succeeded)
        Assert.Equal(2026, repository.LastFinancialYear)
    End Sub

    <Fact>
    Public Sub GetTdsEmployerNps_Fails_WhenThereIsNoRow()
        Dim result = New StatutoryRateService(New FakeStatutoryRateRepository()).GetTdsEmployerNps(AsOf, TdsRegime.Old)
        Assert.False(result.Succeeded)
        Assert.Contains("2026-27", result.Message)
    End Sub

    <Fact>
    Public Sub GetTdsEmployerNps_Fails_WhenTheRowIsUnverified_AndVerificationIsRequired()
        Dim repository As New FakeStatutoryRateRepository() With {.EmployerNps = Row(TdsRegime.New, 14D, False)}
        Assert.False(New StatutoryRateService(repository).GetTdsEmployerNps(AsOf, TdsRegime.New).Succeeded)
    End Sub

    <Fact>
    Public Sub GetTdsEmployerNps_AcceptsAnUnverifiedRow_OnlyWhenVerificationIsNotRequired()
        Dim repository As New FakeStatutoryRateRepository() With {.EmployerNps = Row(TdsRegime.New, 14D, False), .VerificationRequired = False}
        Assert.True(New StatutoryRateService(repository).GetTdsEmployerNps(AsOf, TdsRegime.New).Succeeded)
    End Sub

    <Fact>
    Public Sub TdsEmployerNps_RejectsAPercentOutsideZeroToOneHundred()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() Row(TdsRegime.New, 101D, True))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() Row(TdsRegime.New, -1D, True))
    End Sub
End Class
