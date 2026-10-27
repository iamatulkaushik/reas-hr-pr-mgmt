Imports Revolution.Modules.Payroll
Imports Xunit

Public Class LeaveEncashmentCalculatorTests
    <Fact>
    Public Sub WholeBalance_TimesDailyWage()
        Dim r As LeaveEncashmentResult = LeaveEncashmentCalculator.Calculate(12D, 0D, 1000D)
        Assert.Equal(12D, r.Days)
        Assert.Equal(12000D, r.Amount)
        Assert.False(r.WasCapped)
    End Sub

    <Fact>
    Public Sub BalanceOverLimit_IsCut()
        Dim r As LeaveEncashmentResult = LeaveEncashmentCalculator.Calculate(40D, 30D, 1000D)
        Assert.Equal(30D, r.Days)
        Assert.Equal(30000D, r.Amount)
        Assert.True(r.WasCapped)
    End Sub

    <Fact>
    Public Sub HalfDays_RoundOnceToNearestRupee()
        Assert.Equal(1731D, LeaveEncashmentCalculator.Calculate(1.5D, 0D, 1153.8D).Amount)
    End Sub

    <Fact>
    Public Sub NoBalance_PaysZero()
        Assert.Equal(0D, LeaveEncashmentCalculator.Calculate(0D, 30D, 1000D).Amount)
    End Sub

    <Fact>
    Public Sub NegativeInputs_Throw()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() LeaveEncashmentCalculator.Calculate(-1D, 0D, 1D))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() LeaveEncashmentCalculator.Calculate(1D, -1D, 1D))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() LeaveEncashmentCalculator.Calculate(1D, 0D, -1D))
    End Sub
End Class
