Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class FnfTdsTrueUpTests
    Private Shared Function Earned(ParamArray monthKeys As Integer()) As Dictionary(Of Integer, TdsMonthFacts)
        Dim d As New Dictionary(Of Integer, TdsMonthFacts)()
        For Each key As Integer In monthKeys
            d(key) = New TdsMonthFacts(key, 10000D, 4000D, 30000D)
        Next
        Return d
    End Function

    Private Shared Function Projection(totalTaxSlab As Decimal, deducted As Decimal) As TdsProjectionResult
        Dim annual As New TdsAnnualResult(0D, 0D, 0D, 0D, totalTaxSlab, 0D, 0D, 0D)
        Return New TdsProjectionResult(TdsRegime.New, True, 0D, 0D, annual, deducted, 1, 0D)
    End Function

    <Fact>
    Public Sub Months_AreAprilToMarch_ZeroAfterExit()
        Dim months As IReadOnlyList(Of TdsMonthFacts) = FnfTdsTrueUp.MonthsForExit(2026, 202610, Earned(202604, 202605, 202606, 202607, 202608, 202609, 202610))
        Assert.Equal(12, months.Count)
        Assert.Equal(202604, months(0).MonthKey)
        Assert.Equal(202701, months(9).MonthKey)
        Assert.Equal(202703, months(11).MonthKey)
        Assert.Equal(30000D, months(6).TaxableSalary)
        Assert.Equal(0D, months(7).TaxableSalary)
        Assert.Equal(0D, months(11).TaxableSalary)
    End Sub

    <Fact>
    Public Sub MissingEarnedMonth_IsZero()
        Dim months As IReadOnlyList(Of TdsMonthFacts) = FnfTdsTrueUp.MonthsForExit(2026, 202607, Earned(202604, 202606, 202607))
        Assert.Equal(0D, months(1).TaxableSalary)
        Assert.Equal(30000D, months(2).TaxableSalary)
    End Sub

    <Fact>
    Public Sub EntryAfterExitMonth_IsIgnored()
        Dim months As IReadOnlyList(Of TdsMonthFacts) = FnfTdsTrueUp.MonthsForExit(2026, 202606, Earned(202604, 202605, 202606, 202607))
        Assert.Equal(0D, months(3).TaxableSalary)
    End Sub

    <Fact>
    Public Sub JanuaryExit_UsesTheYearThatStartedInApril()
        Dim months As IReadOnlyList(Of TdsMonthFacts) = FnfTdsTrueUp.MonthsForExit(2026, 202701, Earned(202604, 202701))
        Assert.Equal(30000D, months(9).TaxableSalary)
        Assert.Equal(0D, months(10).TaxableSalary)
    End Sub

    <Fact>
    Public Sub ExitMonthOutsideYear_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() FnfTdsTrueUp.MonthsForExit(2026, 202603, Earned()))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() FnfTdsTrueUp.MonthsForExit(2026, 202704, Earned()))
    End Sub

    <Fact>
    Public Sub TaxMoreThanDeducted_IsAShortfall()
        Assert.Equal(2000D, FnfTdsTrueUp.Adjustment(Projection(12000D, 10000D)))
    End Sub

    <Fact>
    Public Sub TaxLessThanDeducted_IsARefund()
        Assert.Equal(-3000D, FnfTdsTrueUp.Adjustment(Projection(7000D, 10000D)))
    End Sub

    <Fact>
    Public Sub FeedsTheAssembler()
        Dim lines As IReadOnlyList(Of FnfLine) = FnfAssembler.Build(New FnfAssemblyInput(1000D, Nothing, Nothing, Nothing, Nothing, 0D, FnfTdsTrueUp.Adjustment(Projection(7000D, 10000D))))
        Assert.Equal(FnfComponentKind.TdsRefund, lines(1).Kind)
        Assert.Equal(3000D, lines(1).Amount)
    End Sub
End Class
