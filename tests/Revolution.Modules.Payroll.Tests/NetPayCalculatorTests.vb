Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Known answers by hand. EMI cap 25 percent of pay before EMI (Owner).</summary>
Public Class NetPayCalculatorTests
    <Fact>
    Public Sub NoLoan_NetIsGrossLessDeductions()
        Dim r As NetPayResult = NetPayCalculator.Calculate(20000D, 1800D, 200D, 0D, 0D, 25D)
        Assert.Equal(18000D, r.NetPay)
        Assert.Equal(0D, r.EmiRecovered)
    End Sub

    <Fact>
    Public Sub EmiBelowCap_TakenInFull()
        Dim r As NetPayResult = NetPayCalculator.Calculate(20000D, 2000D, 0D, 0D, 3000D, 25D)
        Assert.Equal(3000D, r.EmiRecovered)
        Assert.Equal(0D, r.EmiDeferred)
        Assert.Equal(15000D, r.NetPay)
    End Sub

    <Fact>
    Public Sub EmiAboveCap_LimitedAndRestDeferred()
        Dim r As NetPayResult = NetPayCalculator.Calculate(20000D, 2000D, 0D, 0D, 6000D, 25D)
        Assert.Equal(4500D, r.EmiRecovered)
        Assert.Equal(1500D, r.EmiDeferred)
        Assert.Equal(13500D, r.NetPay)
    End Sub

    <Fact>
    Public Sub CapRoundedDown()
        Dim r As NetPayResult = NetPayCalculator.Calculate(10001D, 0D, 0D, 0D, 5000D, 25D)
        Assert.Equal(2500D, r.EmiRecovered)
        Assert.Equal(7501D, r.NetPay)
    End Sub

    <Fact>
    Public Sub DeductionsAbovePay_NetZeroAndShortfallCarried()
        Dim r As NetPayResult = NetPayCalculator.Calculate(5000D, 1000D, 4500D, 0D, 2000D, 25D)
        Assert.Equal(0D, r.NetPay)
        Assert.Equal(500D, r.CarryForwardOut)
        Assert.Equal(0D, r.EmiRecovered)
        Assert.Equal(2000D, r.EmiDeferred)
    End Sub

    <Fact>
    Public Sub CarryIn_ReducesPay()
        Dim r As NetPayResult = NetPayCalculator.Calculate(10000D, 1000D, 0D, 500D, 0D, 25D)
        Assert.Equal(8500D, r.NetPay)
    End Sub

    <Fact>
    Public Sub ZeroCap_NoEmiTaken()
        Dim r As NetPayResult = NetPayCalculator.Calculate(10000D, 0D, 0D, 0D, 1000D, 0D)
        Assert.Equal(0D, r.EmiRecovered)
        Assert.Equal(1000D, r.EmiDeferred)
    End Sub

    <Fact>
    Public Sub CapAbove100_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() NetPayCalculator.Calculate(1D, 0D, 0D, 0D, 0D, 101D))
    End Sub

    <Fact>
    Public Sub NegativeAmount_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() NetPayCalculator.Calculate(-1D, 0D, 0D, 0D, 0D, 25D))
    End Sub
End Class
