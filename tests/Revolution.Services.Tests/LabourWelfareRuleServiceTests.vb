Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

''' <summary>CR 0012 #5: the labour welfare rule is read like the other rates, and the Haryana contribution works out as the board page says.</summary>
Public Class LabourWelfareRuleServiceTests
    Private Shared ReadOnly AsOf As New Date(2026, 10, 1)

    ''' <summary>Haryana from 1 January 2026: 0.2 percent of wages, at most 35 (employer at most 70), employer twice the employee share.</summary>
    Private Shared Function Haryana(verified As Boolean) As LabourWelfareRule
        Return New LabourWelfareRule(6, New Date(2026, 1, 1), 0.2D, 35D, 2D, 0D, LwfFrequency.Monthly, 0D, New List(Of Integer)(),
                                     verified, If(verified, New Date(2026, 10, 24), CType(Nothing, Date?)), "test")
    End Function

    <Fact>
    Public Sub GetLabourWelfareRule_ReturnsTheVerifiedRow_ForTheState()
        Dim repository As New FakeStatutoryRateRepository() With {.Lwf = Haryana(True)}
        Dim result = New StatutoryRateService(repository).GetLabourWelfareRule(6, AsOf)
        Assert.True(result.Succeeded)
        Assert.Equal(6, repository.LastStateId)
        Assert.Equal(AsOf, repository.LastAsOf)
    End Sub

    <Fact>
    Public Sub GetLabourWelfareRule_FailsOnAMissingRow()
        Dim result = New StatutoryRateService(New FakeStatutoryRateRepository()).GetLabourWelfareRule(6, AsOf)
        Assert.False(result.Succeeded)
    End Sub

    <Fact>
    Public Sub GetLabourWelfareRule_FailsOnAnUnverifiedRow_WhenVerificationIsRequired()
        Dim repository As New FakeStatutoryRateRepository() With {.Lwf = Haryana(False)}
        Assert.False(New StatutoryRateService(repository).GetLabourWelfareRule(6, AsOf).Succeeded)
    End Sub

    <Fact>
    Public Sub GetLabourWelfareRule_AcceptsAnUnverifiedRow_OnlyWhenVerificationIsNotRequired()
        Dim repository As New FakeStatutoryRateRepository() With {.Lwf = Haryana(False), .VerificationRequired = False}
        Assert.True(New StatutoryRateService(repository).GetLabourWelfareRule(6, AsOf).Succeeded)
    End Sub

    <Theory>
    <InlineData(10000, 20, 40)>
    <InlineData(17500, 35, 70)>
    <InlineData(20000, 35, 70)>
    <InlineData(0, 0, 0)>
    Public Sub Contribution_IsTwoTenthsPercent_CappedAt35_EmployerTwice(wage As Integer, employee As Integer, employer As Integer)
        Dim rule = Haryana(True)
        Assert.Equal(CDec(employee), rule.EmployeeContribution(CDec(wage)))
        Assert.Equal(CDec(employer), rule.EmployerContribution(CDec(wage)))
    End Sub

    <Fact>
    Public Sub Contribution_IsZeroAboveTheWageCutoff()
        Dim rule As New LabourWelfareRule(6, New Date(2026, 1, 1), 0.2D, 35D, 2D, 0D, LwfFrequency.Monthly, 15000D, New List(Of Integer)(), True, Nothing, "test")
        Assert.Equal(0D, rule.EmployeeContribution(15001D))
        Assert.Equal(0D, rule.EmployerContribution(15001D))
        Assert.Equal(30D, rule.EmployeeContribution(15000D))
    End Sub

    <Fact>
    Public Sub FlatRule_UsesTheFlatAmounts()
        Dim rule As New LabourWelfareRule(27, New Date(2025, 4, 1), 0D, 12D, 0D, 36D, LwfFrequency.HalfYearly, 0D, New List(Of Integer) From {6, 12}, True, Nothing, "test")
        Assert.Equal(12D, rule.EmployeeContribution(9000D))
        Assert.Equal(36D, rule.EmployerContribution(9000D))
        Assert.True(rule.IsDue(6))
        Assert.False(rule.IsDue(7))
    End Sub

    <Fact>
    Public Sub IsDue_MonthlyRule_IsDueEveryMonth_AndRejectsABadMonth()
        Dim rule = Haryana(True)
        Assert.True(rule.IsDue(1))
        Assert.True(rule.IsDue(12))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() rule.IsDue(13))
    End Sub
End Class
