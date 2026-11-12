Imports Revolution.Modules.Assets
Imports Xunit

Public Class AssetDepreciationTests
    <Theory>
    <InlineData("2026-01-15", "2026-11-12", 9)>
    <InlineData("2026-01-15", "2026-11-15", 10)>
    <InlineData("2026-01-31", "2026-02-28", 0)>
    <InlineData("2026-01-15", "2026-01-15", 0)>
    <InlineData("2026-06-01", "2026-01-01", 0)>
    <InlineData("2025-12-20", "2026-01-20", 1)>
    Public Sub CompleteMonths_CountsWholeMonthsOnly(bought As String, asOf As String, expected As Integer)
        Assert.Equal(expected, AssetDepreciation.CompleteMonths(Date.Parse(bought, Globalization.CultureInfo.InvariantCulture), Date.Parse(asOf, Globalization.CultureInfo.InvariantCulture)))
    End Sub

    <Fact>
    Public Sub ValueOn_NineMonthsOfThirtySix_LeavesThreeQuarters()
        Assert.Equal(45000D, AssetDepreciation.ValueOn(60000D, New Date(2026, 1, 15), 36, New Date(2026, 11, 12)))
    End Sub

    <Fact>
    Public Sub ValueOn_RoundsToWholeRupees()
        Assert.Equal(43333D, AssetDepreciation.ValueOn(60000D, New Date(2026, 1, 15), 36, New Date(2026, 11, 15)))
    End Sub

    <Fact>
    Public Sub ValueOn_BeforeOrOnPurchaseDay_IsTheFullValue()
        Assert.Equal(60000D, AssetDepreciation.ValueOn(60000D, New Date(2026, 1, 15), 36, New Date(2026, 1, 1)))
        Assert.Equal(60000D, AssetDepreciation.ValueOn(60000D, New Date(2026, 1, 15), 36, New Date(2026, 1, 15)))
    End Sub

    <Fact>
    Public Sub ValueOn_AtOrAfterTheEndOfLife_IsZero()
        Assert.Equal(0D, AssetDepreciation.ValueOn(60000D, New Date(2026, 1, 15), 36, New Date(2029, 1, 15)))
        Assert.Equal(0D, AssetDepreciation.ValueOn(60000D, New Date(2026, 1, 15), 36, New Date(2035, 1, 1)))
    End Sub

    <Fact>
    Public Sub ValueOn_NeedsALifeOfAtLeastOneMonth()
        Assert.Throws(Of ArgumentOutOfRangeException)(Sub() AssetDepreciation.ValueOn(1000D, New Date(2026, 1, 1), 0, New Date(2026, 2, 1)))
    End Sub
End Class
