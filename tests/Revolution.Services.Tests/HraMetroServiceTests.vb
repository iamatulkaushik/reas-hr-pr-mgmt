Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

''' <summary>CR 0012 #2: the HRA city list. A missing list, a list without the "*" row, or an unsigned row is a failure, never an assumed percentage.</summary>
Public Class HraMetroServiceTests
    Private Shared ReadOnly AsOf As New Date(2026, 10, 1)
    Private Shared ReadOnly ListFrom As New Date(2026, 4, 1)

    Private Shared Function City(name As String, metro As Boolean, pct As Decimal, verified As Boolean) As HraCity
        Return New HraCity(ListFrom, name, metro, pct, verified, If(verified, New Date(2026, 4, 2), CType(Nothing, Date?)), "test")
    End Function

    Private Shared Function RepoWith(ParamArray rows As HraCity()) As FakeStatutoryRateRepository
        Return New FakeStatutoryRateRepository() With {.HraCities = rows}
    End Function

    Private Shared Function VerifiedList() As FakeStatutoryRateRepository
        Return RepoWith(City("Mumbai", True, 50D, True), City("Delhi", True, 50D, True), City(HraCity.OtherCities, False, 40D, True))
    End Function

    <Fact>
    Public Sub GetHraMetroList_ReturnsTheListInForce()
        Dim repository = VerifiedList()
        Dim result = New StatutoryRateService(repository).GetHraMetroList(AsOf)
        Assert.True(result.Succeeded)
        Assert.Equal(3, result.Value.Rows.Count)
        Assert.Equal(ListFrom, result.Value.EffectiveFrom)
        Assert.Equal(AsOf, repository.LastAsOf)
    End Sub

    <Fact>
    Public Sub ExemptPctFor_MatchesACityIgnoringCaseAndSpaces_AndFallsBackToTheOtherCitiesRow()
        Dim list = New StatutoryRateService(VerifiedList()).GetHraMetroList(AsOf).Value
        Assert.Equal(50D, list.ExemptPctFor("mumbai"))
        Assert.Equal(50D, list.ExemptPctFor("  DELHI "))
        Assert.Equal(40D, list.ExemptPctFor("Nagpur"))
        Assert.Equal(40D, list.ExemptPctFor(Nothing))
        Assert.True(list.IsListed("Mumbai"))
        Assert.False(list.IsListed("Nagpur"))
    End Sub

    <Fact>
    Public Sub GetHraMetroList_Fails_WhenThereIsNoList()
        Dim result = New StatutoryRateService(RepoWith()).GetHraMetroList(AsOf)
        Assert.False(result.Succeeded)
        Assert.NotEqual(String.Empty, result.Message)
    End Sub

    <Fact>
    Public Sub GetHraMetroList_Fails_WhenTheListHasNoRowForOtherCities()
        Dim result = New StatutoryRateService(RepoWith(City("Mumbai", True, 50D, True))).GetHraMetroList(AsOf)
        Assert.False(result.Succeeded)
        Assert.Contains("other cities", result.Message)
    End Sub

    <Fact>
    Public Sub GetHraMetroList_Fails_WhenAnyRowIsNotVerified()
        Dim repository = RepoWith(City("Mumbai", True, 50D, False), City(HraCity.OtherCities, False, 40D, True))
        Assert.False(New StatutoryRateService(repository).GetHraMetroList(AsOf).Succeeded)
    End Sub

    <Fact>
    Public Sub GetHraMetroList_AcceptsUnverifiedRows_OnlyWhenVerificationIsNotRequired()
        Dim repository = RepoWith(City("Mumbai", True, 50D, False), City(HraCity.OtherCities, False, 40D, False))
        repository.VerificationRequired = False
        Assert.True(New StatutoryRateService(repository).GetHraMetroList(AsOf).Succeeded)
    End Sub
End Class
