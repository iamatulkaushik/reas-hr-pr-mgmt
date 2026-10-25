Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

''' <summary>CR 0012 #6: the minimum wage rows are read like the other rates. A missing or unverified table is a failure, never a default.</summary>
Public Class MinimumWageServiceTests
    Private Shared ReadOnly AsOf As New Date(2026, 10, 1)

    Private Shared Function Rows(verified As Boolean, Optional oneUnverified As Boolean = False) As IReadOnlyList(Of MinimumWageRate)
        Dim list As New List(Of MinimumWageRate)()
        Dim amounts() As Decimal = {15220.71D, 16780.74D, 18500.81D, 19425.85D}
        For i = 0 To 3
            Dim isVerified = verified AndAlso Not (oneUnverified AndAlso i = 3)
            list.Add(New MinimumWageRate(6, New Date(2026, 4, 1), 1, CType(i + 1, WageSkill), WageUnit.PerMonth, amounts(i), isVerified, Nothing, "test"))
        Next
        Return list
    End Function

    <Fact>
    Public Sub GetMinimumWages_ReturnsTheVerifiedRows_ForTheState()
        Dim repository As New FakeStatutoryRateRepository() With {.MinimumWages = Rows(True)}
        Dim result = New StatutoryRateService(repository).GetMinimumWages(6, AsOf)
        Assert.True(result.Succeeded)
        Assert.Equal(4, result.Value.Count)
        Assert.Equal(6, repository.LastStateId)
        Assert.Equal(15220.71D, result.Value(0).Amount)
        Assert.Equal(WageSkill.Unskilled, result.Value(0).Skill)
    End Sub

    <Fact>
    Public Sub GetMinimumWages_FailsWhenThereAreNoRows()
        Assert.False(New StatutoryRateService(New FakeStatutoryRateRepository()).GetMinimumWages(6, AsOf).Succeeded)
    End Sub

    <Fact>
    Public Sub GetMinimumWages_FailsWhenAnyRowIsUnverified_AndVerificationIsRequired()
        Dim repository As New FakeStatutoryRateRepository() With {.MinimumWages = Rows(True, oneUnverified:=True)}
        Assert.False(New StatutoryRateService(repository).GetMinimumWages(6, AsOf).Succeeded)
    End Sub

    <Fact>
    Public Sub GetMinimumWages_AcceptsUnverifiedRows_OnlyWhenVerificationIsNotRequired()
        Dim repository As New FakeStatutoryRateRepository() With {.MinimumWages = Rows(False), .VerificationRequired = False}
        Assert.True(New StatutoryRateService(repository).GetMinimumWages(6, AsOf).Succeeded)
    End Sub
End Class
