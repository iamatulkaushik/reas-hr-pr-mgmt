Imports Revolution.Core
Imports Xunit

Public Class EpfPeriodSplitterTests
    Private Shared ReadOnly OldFrom As New Date(2014, 9, 1)
    Private Shared ReadOnly NewFrom As New Date(2026, 9, 17)

    Private Shared Function Rate(effectiveFrom As Date, ceiling As Decimal) As EpfRate
        Return New EpfRate(effectiveFrom, 12D, 3.67D, 8.33D, 0.5D, 0.5D, 500D, ceiling, ceiling, True, "test")
    End Function

    Private Shared Function Lookup(asOf As Date) As OperationResult(Of EpfRate)
        Return OperationResult(Of EpfRate).Ok(If(asOf >= NewFrom, Rate(NewFrom, 25000D), Rate(OldFrom, 15000D)))
    End Function

    <Fact>
    Public Sub Split_MonthWithoutChange_IsOnePart()
        Dim result = EpfPeriodSplitter.Split(New Date(2026, 10, 1), New Date(2026, 10, 31), AddressOf Lookup)
        Assert.True(result.Succeeded)
        Assert.Single(result.Value)
        Assert.Equal(31, result.Value(0).Days)
        Assert.Equal(25000D, result.Value(0).Rate.WageCeiling)
    End Sub

    <Fact>
    Public Sub Split_September2026_IsTwoPartsAtTheChange()
        Dim result = EpfPeriodSplitter.Split(New Date(2026, 9, 1), New Date(2026, 9, 30), AddressOf Lookup)
        Assert.True(result.Succeeded)
        Assert.Equal(2, result.Value.Count)
        Assert.Equal(16, result.Value(0).Days)
        Assert.Equal(15000D, result.Value(0).Rate.WageCeiling)
        Assert.Equal(New Date(2026, 9, 17), result.Value(1).FromDate)
        Assert.Equal(14, result.Value(1).Days)
        Assert.Equal(25000D, result.Value(1).Rate.WageCeiling)
    End Sub

    <Fact>
    Public Sub Split_RateFailure_IsReturnedAsIs()
        Dim result = EpfPeriodSplitter.Split(New Date(2026, 9, 1), New Date(2026, 9, 30),
                                             Function(d) OperationResult(Of EpfRate).Fail("No EPF rate."))
        Assert.False(result.Succeeded)
        Assert.Equal("No EPF rate.", result.Message)
    End Sub

    <Fact>
    Public Sub Split_PeriodEndsBeforeItStarts_Throws()
        Assert.Throws(Of ArgumentException)(Function() EpfPeriodSplitter.Split(New Date(2026, 9, 30), New Date(2026, 9, 1), AddressOf Lookup))
    End Sub
End Class
