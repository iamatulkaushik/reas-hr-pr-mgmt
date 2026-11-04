Imports Revolution.Modules.Arrears
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class ArrearsRunBuilderTests
    Private Shared Function Month(monthNo As Integer, heads As List(Of ArrearsHeadAmount), gross As Decimal, epfEmployee As Decimal) As ArrearsMonthRecord
        Return New ArrearsMonthRecord(New Date(2026, monthNo, 1), 0D, gross, gross, gross, gross, gross, epfEmployee, 73D, 167D, 10D, 10D, 0D, 0D, heads, 0D, 0D)
    End Function

    Private Shared Function Claim(id As Integer, employee As Integer, code As String, ParamArray months As ArrearsMonthRecord()) As ArrearsClaimDetail
        Dim row As New ArrearsClaimRow(id, employee, code, "Name " & code, months.First().WageMonth, months.Last().WageMonth, "Revision", ArrearsClaimStatus.Open,
                                       months.Sum(Function(m) m.GrossDifference), 0D, 0D, 0D, New Date(2026, 11, 1))
        Return New ArrearsClaimDetail(row, months)
    End Function

    Private Shared Function Heads(basic As Decimal, da As Decimal) As List(Of ArrearsHeadAmount)
        Return New List(Of ArrearsHeadAmount) From {New ArrearsHeadAmount(1, basic), New ArrearsHeadAmount(2, da)}
    End Function

    Private Shared Function Factories(ParamArray ids As Integer()) As Dictionary(Of Integer, Integer)
        Return ids.ToDictionary(Function(i) i, Function(i) 7)
    End Function

    <Fact>
    Public Sub OneClaim_BecomesOneLine_HeadByHead_WithNoTax()
        Dim plan As ArrearsRunPlan = ArrearsRunBuilder.Build(New List(Of ArrearsClaimDetail) From {Claim(11, 101, "E101", Month(5, Heads(1500D, 500D), 2000D, 240D))}, Factories(101))
        Assert.Empty(plan.Problems)
        Dim line As RunLineResult = plan.Lines.Single()
        Assert.Equal(101, line.EmployeeId)
        Assert.Equal(7, line.FactoryId)
        Assert.Equal(2000D, line.GrossEarnings)
        Assert.Equal(240D, line.EpfEmployee)
        Assert.Equal(1760D, line.NetPay)
        Assert.Equal(240D, line.TotalDeductions)
        Assert.Equal(0D, line.TdsAmount)
        Assert.Equal(0D, line.ProfessionalTax)
        Assert.Equal(0D, line.PaidDays)
        Assert.Equal(2, line.Earnings.Count)
        Assert.Equal(1500D, line.Earnings.Single(Function(e) e.ComponentId = 1).Amount)
        Assert.Equal(11, plan.ClaimIds.Single())
    End Sub

    <Fact>
    Public Sub TwoClaimsOfOneEmployee_AreMergedIntoOneLine()
        Dim plan As ArrearsRunPlan = ArrearsRunBuilder.Build(New List(Of ArrearsClaimDetail) From {
            Claim(11, 101, "E101", Month(5, Heads(1500D, 500D), 2000D, 240D)), Claim(12, 101, "E101", Month(6, Heads(1000D, 0D), 1000D, 120D))}, Factories(101))
        Dim line As RunLineResult = plan.Lines.Single()
        Assert.Equal(3000D, line.GrossEarnings)
        Assert.Equal(2500D, line.Earnings.Single(Function(e) e.ComponentId = 1).Amount)
        Assert.Equal(2, plan.ClaimIds.Count)
    End Sub

    <Fact>
    Public Sub Overpayment_IsNotInTheRun_AndIsNamedForRecovery()
        Dim plan As ArrearsRunPlan = ArrearsRunBuilder.Build(New List(Of ArrearsClaimDetail) From {
            Claim(11, 101, "E101", Month(5, Heads(-300D, 0D), -300D, -36D)), Claim(12, 102, "E102", Month(5, Heads(1000D, 0D), 1000D, 120D))}, Factories(101, 102))
        Assert.Equal(1, plan.Lines.Count)
        Assert.Equal(102, plan.Lines.Single().EmployeeId)
        Assert.Equal("E101", plan.Recoveries.Single())
        Assert.Empty(plan.Problems)
        Assert.Equal(12, plan.ClaimIds.Single())
    End Sub

    <Fact>
    Public Sub OneHeadBelowZero_LeavesTheClaimOut()
        Dim plan As ArrearsRunPlan = ArrearsRunBuilder.Build(New List(Of ArrearsClaimDetail) From {Claim(11, 101, "E101", Month(5, Heads(2500D, -500D), 2000D, 240D))}, Factories(101))
        Assert.Empty(plan.Lines)
        Assert.Single(plan.Problems)
    End Sub

    <Fact>
    Public Sub ClaimWithoutHeadAmounts_IsLeftOut()
        Dim plan As ArrearsRunPlan = ArrearsRunBuilder.Build(New List(Of ArrearsClaimDetail) From {Claim(11, 101, "E101", Month(5, New List(Of ArrearsHeadAmount)(), 2000D, 240D))}, Factories(101))
        Assert.Empty(plan.Lines)
        Assert.Contains("head amounts", plan.Problems.Single(), StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub EmployeeWithoutPayFacts_IsLeftOut()
        Dim plan As ArrearsRunPlan = ArrearsRunBuilder.Build(New List(Of ArrearsClaimDetail) From {Claim(11, 101, "E101", Month(5, Heads(1500D, 500D), 2000D, 240D))}, Factories(999))
        Assert.Empty(plan.Lines)
        Assert.Contains("E101", plan.Problems.Single(), StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub NetBelowZero_IsAnOverpayment_ForRecovery()
        Dim plan As ArrearsRunPlan = ArrearsRunBuilder.Build(New List(Of ArrearsClaimDetail) From {Claim(11, 101, "E101", Month(5, Heads(100D, 0D), 100D, 240D))}, Factories(101))
        Assert.Empty(plan.Lines)
        Assert.Equal("E101", plan.Recoveries.Single())
        Assert.Empty(plan.ClaimIds)
    End Sub

    <Fact>
    Public Sub OneLowerMonth_IsFine_WhenTheClaimAsAWholeIsPositive()
        Dim plan As ArrearsRunPlan = ArrearsRunBuilder.Build(New List(Of ArrearsClaimDetail) From {
            Claim(11, 101, "E101", Month(5, Heads(2000D, 0D), 2000D, 240D), Month(6, Heads(-500D, 0D), -500D, -60D))}, Factories(101))
        Assert.Empty(plan.Problems)
        Assert.Empty(plan.Recoveries)
        Assert.Equal(1500D, plan.Lines.Single().GrossEarnings)
        Assert.Equal(1500D, plan.Lines.Single().Earnings.Single(Function(e) e.ComponentId = 1).Amount)
    End Sub

    <Fact>
    Public Sub OverpaidClaim_NeedsNoPayFacts()
        Dim plan As ArrearsRunPlan = ArrearsRunBuilder.Build(New List(Of ArrearsClaimDetail) From {Claim(11, 101, "E101", Month(5, Heads(-300D, 0D), -300D, -36D))}, Factories(999))
        Assert.Equal("E101", plan.Recoveries.Single())
        Assert.Empty(plan.Problems)
    End Sub

    <Fact>
    Public Sub AllZeroClaim_IsSkippedQuietly()
        Dim zero As New ArrearsMonthRecord(New Date(2026, 5, 1), 0D, 0D, 0D, 0D, 0D, 0D, 0D, 0D, 0D, 0D, 0D, 0D, 0D, New List(Of ArrearsHeadAmount)(), 0D, 0D)
        Dim plan As ArrearsRunPlan = ArrearsRunBuilder.Build(New List(Of ArrearsClaimDetail) From {Claim(11, 101, "E101", zero)}, Factories(101))
        Assert.Empty(plan.Lines)
        Assert.Empty(plan.Problems)
        Assert.Empty(plan.ClaimIds)
    End Sub

    <Fact>
    Public Sub HeadsThatNetToZero_AreNotPrinted()
        Dim plan As ArrearsRunPlan = ArrearsRunBuilder.Build(New List(Of ArrearsClaimDetail) From {
            Claim(11, 101, "E101", Month(5, Heads(1000D, 0D), 1000D, 120D))}, Factories(101))
        Assert.Equal(1, plan.Lines.Single().Earnings.Count)
    End Sub
End Class
