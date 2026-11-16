Imports Revolution.Modules.Payroll
Imports Xunit

Public Class EsiContributionPeriodTests
    <Theory>
    <InlineData(2026, 3, 31, 2025, 10, 1)>
    <InlineData(2026, 4, 1, 2026, 4, 1)>
    <InlineData(2026, 4, 25, 2026, 4, 1)>
    <InlineData(2026, 9, 30, 2026, 4, 1)>
    <InlineData(2026, 10, 1, 2026, 10, 1)>
    <InlineData(2027, 1, 15, 2026, 10, 1)>
    Public Sub StartOf_GivesAprilOrOctober(y As Integer, m As Integer, d As Integer, ey As Integer, em As Integer, ed As Integer)
        Assert.Equal(New Date(ey, em, ed), EsiContributionPeriod.StartOf(New Date(y, m, d)))
    End Sub

    <Theory>
    <InlineData(2026, 4, 25, 2026, 9, 30)>
    <InlineData(2026, 11, 15, 2027, 3, 31)>
    <InlineData(2027, 3, 31, 2027, 3, 31)>
    Public Sub EndOf_GivesSeptemberOrMarch(y As Integer, m As Integer, d As Integer, ey As Integer, em As Integer, ed As Integer)
        Assert.Equal(New Date(ey, em, ed), EsiContributionPeriod.EndOf(New Date(y, m, d)))
    End Sub

    <Fact>
    Public Sub SamePeriod_InsideOnePeriod_IsTrue()
        Assert.True(EsiContributionPeriod.SamePeriod(New Date(2026, 4, 1), New Date(2026, 9, 30)))
        Assert.True(EsiContributionPeriod.SamePeriod(New Date(2026, 10, 2), New Date(2027, 1, 15)))
    End Sub

    <Fact>
    Public Sub SamePeriod_AcrossTheBoundary_IsFalse()
        Assert.False(EsiContributionPeriod.SamePeriod(New Date(2026, 9, 30), New Date(2026, 10, 1)))
        Assert.False(EsiContributionPeriod.SamePeriod(New Date(2027, 3, 31), New Date(2027, 4, 1)))
    End Sub
End Class
