Imports Revolution.Modules.Payroll
Imports Xunit

Public Class FnfCalculatorTests
    Private Shared Function Lines(ParamArray items As FnfLine()) As IReadOnlyList(Of FnfLine)
        Return items.ToList()
    End Function

    <Fact>
    Public Sub EarningsAndDeductions_AddOnTheirOwnSide()
        Dim totals As FnfTotals = FnfCalculator.Total(Lines(
            New FnfLine(FnfComponentKind.LastSalary, "Salary to exit", 20000D),
            New FnfLine(FnfComponentKind.Bonus, "Bonus", 3499D),
            New FnfLine(FnfComponentKind.Gratuity, "Gratuity", 173077D),
            New FnfLine(FnfComponentKind.NoticePayRecovery, "Notice pay", 20000D),
            New FnfLine(FnfComponentKind.LoanClosure, "Loan", 5000D)))
        Assert.Equal(196576D, totals.Earnings)
        Assert.Equal(25000D, totals.Deductions)
        Assert.Equal(171576D, totals.Net)
        Assert.False(totals.IsRecoverable)
    End Sub

    <Fact>
    Public Sub DeductionsMoreThanEarnings_IsRecoverable()
        Dim totals As FnfTotals = FnfCalculator.Total(Lines(
            New FnfLine(FnfComponentKind.LastSalary, "Salary", 5000D),
            New FnfLine(FnfComponentKind.NoticePayRecovery, "Notice", 9000D)))
        Assert.Equal(-4000D, totals.Net)
        Assert.True(totals.IsRecoverable)
    End Sub

    <Fact>
    Public Sub ArrearsRecovery_IsADeduction()
        Assert.True(FnfCalculator.IsDeduction(FnfComponentKind.ArrearsRecovery))
    End Sub

    <Fact>
    Public Sub TdsRefund_IsAnEarning()
        Assert.False(FnfCalculator.IsDeduction(FnfComponentKind.TdsRefund))
        Assert.True(FnfCalculator.IsDeduction(FnfComponentKind.TdsShortfall))
    End Sub

    <Fact>
    Public Sub NoLines_TotalsZero()
        Dim totals As FnfTotals = FnfCalculator.Total(New List(Of FnfLine)())
        Assert.Equal(0D, totals.Net)
    End Sub

    <Fact>
    Public Sub BadLine_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New FnfLine(FnfComponentKind.Bonus, "Bonus", 0D))
        Assert.Throws(Of ArgumentException)(Function() New FnfLine(FnfComponentKind.Bonus, " ", 10D))
    End Sub
End Class
