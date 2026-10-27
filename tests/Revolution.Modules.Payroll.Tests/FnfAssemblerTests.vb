Imports Revolution.Modules.Payroll
Imports Xunit

Public Class FnfAssemblerTests
    Private Shared Function Gratuity(amount As Decimal) As GratuityResult
        Return New GratuityResult(True, String.Empty, 8, 0, 8, amount, False)
    End Function

    Private Shared Function Full() As FnfAssemblyInput
        Return New FnfAssemblyInput(20000D,
                                    NoticePayCalculator.Calculate(30, 10, 1000D),
                                    New BonusResult(True, String.Empty, 42000D, 8.33D, 3499D, 0D, 3499D),
                                    LeaveEncashmentCalculator.Calculate(12D, 0D, 1000D),
                                    Gratuity(173077D), 5000D, 0D, 0D)
    End Function

    <Fact>
    Public Sub FullSettlement_OrderAndTotals()
        Dim lines As IReadOnlyList(Of FnfLine) = FnfAssembler.Build(Full())
        Assert.Equal(6, lines.Count)
        Assert.Equal(FnfComponentKind.LastSalary, lines(0).Kind)
        Assert.Equal(FnfComponentKind.NoticePayRecovery, lines(4).Kind)
        Dim totals As FnfTotals = FnfCalculator.Total(lines)
        Assert.Equal(208576D, totals.Earnings)
        Assert.Equal(25000D, totals.Deductions)
        Assert.Equal(183576D, totals.Net)
    End Sub

    <Fact>
    Public Sub NotEligibleBonusAndGratuity_AddNoLine()
        Dim input As New FnfAssemblyInput(5000D, Nothing,
                                          New BonusResult(False, "no", 0D, 8.33D, 0D, 0D, 0D), Nothing,
                                          New GratuityResult(False, "under five years", 3, 0, 0, 0D, False))
        Dim lines As IReadOnlyList(Of FnfLine) = FnfAssembler.Build(input)
        Assert.Equal(1, lines.Count)
    End Sub

    <Fact>
    Public Sub ZeroAmounts_AreSkipped()
        Dim input As New FnfAssemblyInput(0D, NoticePayCalculator.Calculate(30, 30, 1000D), Nothing, LeaveEncashmentCalculator.Calculate(0D, 0D, 1000D))
        Assert.Equal(0, FnfAssembler.Build(input).Count)
    End Sub

    <Fact>
    Public Sub TdsRefund_IsAnEarning_Shortfall_IsADeduction()
        Dim refund As IReadOnlyList(Of FnfLine) = FnfAssembler.Build(New FnfAssemblyInput(1000D, Nothing, Nothing, Nothing, Nothing, 0D, -1500D))
        Assert.Equal(FnfComponentKind.TdsRefund, refund(1).Kind)
        Assert.Equal(1500D, refund(1).Amount)
        Dim shortfall As IReadOnlyList(Of FnfLine) = FnfAssembler.Build(New FnfAssemblyInput(1000D, Nothing, Nothing, Nothing, Nothing, 0D, 700D))
        Assert.Equal(FnfComponentKind.TdsShortfall, shortfall(1).Kind)
        Assert.True(shortfall(1).IsDeduction)
    End Sub

    <Fact>
    Public Sub ArrearsRecovery_OnlyWhenTheOwnerChoosesIt()
        Assert.Equal(1, FnfAssembler.Build(New FnfAssemblyInput(1000D)).Count)
        Dim lines As IReadOnlyList(Of FnfLine) = FnfAssembler.Build(New FnfAssemblyInput(1000D, Nothing, Nothing, Nothing, Nothing, 0D, 0D, 400D))
        Assert.Equal(FnfComponentKind.ArrearsRecovery, lines(1).Kind)
        Assert.Equal(600D, FnfCalculator.Total(lines).Net)
    End Sub

    <Fact>
    Public Sub NegativeInputs_Throw()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New FnfAssemblyInput(-1D))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New FnfAssemblyInput(1D, Nothing, Nothing, Nothing, Nothing, -1D))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New FnfAssemblyInput(1D, Nothing, Nothing, Nothing, Nothing, 0D, 0D, -1D))
    End Sub
End Class
