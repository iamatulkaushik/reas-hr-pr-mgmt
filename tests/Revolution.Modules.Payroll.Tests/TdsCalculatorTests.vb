Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Known answers worked by hand from the fixture rows. The CA signs the expected values.</summary>
Public Class TdsCalculatorTests
    <Fact>
    Public Sub NewRegime_TwelveLakhTaxable_RebateClearsTax()
        Dim r As TdsAnnualResult = TdsCalculator.Annual(1275000D, Nothing, TdsTestRules.NewRegime(), Nothing)
        Assert.Equal(1200000D, r.TaxableIncome)
        Assert.Equal(60000D, r.SlabTax)
        Assert.Equal(0D, r.TotalTax)
    End Sub

    <Fact>
    Public Sub NewRegime_JustAboveLimit_MarginalReliefCapsTaxAtExcess()
        Dim r As TdsAnnualResult = TdsCalculator.Annual(1300000D, Nothing, TdsTestRules.NewRegime(), Nothing)
        Assert.Equal(63750D, r.SlabTax)
        Assert.Equal(25000D, r.SlabTax - r.Rebate)
        Assert.Equal(1000D, r.Cess)
        Assert.Equal(26000D, r.TotalTax)
    End Sub

    <Fact>
    Public Sub NewRegime_TwentyLakhTaxable_NoRebate()
        Dim r As TdsAnnualResult = TdsCalculator.Annual(2075000D, Nothing, TdsTestRules.NewRegime(), Nothing)
        Assert.Equal(200000D, r.SlabTax)
        Assert.Equal(8000D, r.Cess)
        Assert.Equal(208000D, r.TotalTax)
    End Sub

    <Fact>
    Public Sub NewRegime_IgnoresDeclaredDeductions()
        Dim claims As New List(Of TdsDeductionClaim) From {New TdsDeductionClaim("80C", "", 150000D)}
        Dim r As TdsAnnualResult = TdsCalculator.Annual(2075000D, claims, TdsTestRules.NewRegime(), TdsTestRules.OldLimits())
        Assert.Equal(0D, r.Deductions)
        Assert.Equal(2000000D, r.TaxableIncome)
    End Sub

    <Fact>
    Public Sub OldRegime_DeductionsReduceIncome_NoRebateAboveLimit()
        Dim claims As New List(Of TdsDeductionClaim) From {New TdsDeductionClaim("80C", "", 150000D)}
        Dim r As TdsAnnualResult = TdsCalculator.Annual(800000D, claims, TdsTestRules.OldRegime(), TdsTestRules.OldLimits())
        Assert.Equal(600000D, r.TaxableIncome)
        Assert.Equal(32500D, r.SlabTax)
        Assert.Equal(0D, r.Rebate)
        Assert.Equal(33800D, r.TotalTax)
    End Sub

    <Fact>
    Public Sub OldRegime_AtLimit_RebateClearsTax()
        Dim r As TdsAnnualResult = TdsCalculator.Annual(550000D, Nothing, TdsTestRules.OldRegime(), Nothing)
        Assert.Equal(500000D, r.TaxableIncome)
        Assert.Equal(0D, r.TotalTax)
    End Sub

    <Fact>
    Public Sub OldRegime_AboveLimit_NoMarginalRelief()
        Dim r As TdsAnnualResult = TdsCalculator.Annual(560000D, Nothing, TdsTestRules.OldRegime(), Nothing)
        Assert.Equal(14500D, r.SlabTax)
        Assert.Equal(15080D, r.TotalTax)
    End Sub

    <Fact>
    Public Sub OldRegime_ClaimAboveCap_IsCapped()
        Dim claims As New List(Of TdsDeductionClaim) From {
            New TdsDeductionClaim("80C", "", 100000D), New TdsDeductionClaim("80c", "", 100000D)}
        Dim r As TdsAnnualResult = TdsCalculator.Annual(800000D, claims, TdsTestRules.OldRegime(), TdsTestRules.OldLimits())
        Assert.Equal(150000D, r.Deductions)
    End Sub

    <Fact>
    Public Sub OldRegime_SectionWithNoLimitRow_CountsAsZero()
        Dim claims As New List(Of TdsDeductionClaim) From {New TdsDeductionClaim("80E", "", 90000D)}
        Dim r As TdsAnnualResult = TdsCalculator.Annual(800000D, claims, TdsTestRules.OldRegime(), TdsTestRules.OldLimits())
        Assert.Equal(0D, r.Deductions)
    End Sub

    <Fact>
    Public Sub OldRegime_SubLimitsAreSeparate()
        Dim claims As New List(Of TdsDeductionClaim) From {
            New TdsDeductionClaim("80D", "self", 40000D), New TdsDeductionClaim("80D", "parent", 10000D)}
        Dim r As TdsAnnualResult = TdsCalculator.Annual(800000D, claims, TdsTestRules.OldRegime(), TdsTestRules.OldLimits())
        Assert.Equal(25000D, r.Deductions)
    End Sub

    <Fact>
    Public Sub Surcharge_SixtyLakh_TenPercent()
        Dim r As TdsAnnualResult = TdsCalculator.Annual(6075000D, Nothing, TdsTestRules.NewRegime(), Nothing)
        Assert.Equal(1380000D, r.SlabTax)
        Assert.Equal(138000D, r.Surcharge)
        Assert.Equal(1578720D, r.TotalTax)
    End Sub

    <Fact>
    Public Sub Surcharge_JustAboveFiftyLakh_MarginalReliefLimitsIt()
        Dim r As TdsAnnualResult = TdsCalculator.Annual(5085000D, Nothing, TdsTestRules.NewRegime(), Nothing)
        Assert.Equal(5010000D, r.TaxableIncome)
        Assert.Equal(7000D, r.Surcharge)
        Assert.Equal(1133600D, r.TotalTax)
    End Sub

    <Fact>
    Public Sub NoSurchargeBands_NoSurcharge()
        Dim r As TdsAnnualResult = TdsCalculator.Annual(6075000D, Nothing, TdsTestRules.OldRegime(), Nothing)
        Assert.Equal(0D, r.Surcharge)
    End Sub

    <Fact>
    Public Sub Income_BelowStandardDeduction_ZeroTax()
        Dim r As TdsAnnualResult = TdsCalculator.Annual(50000D, Nothing, TdsTestRules.NewRegime(), Nothing)
        Assert.Equal(0D, r.TaxableIncome)
        Assert.Equal(0D, r.TotalTax)
    End Sub

    <Fact>
    Public Sub Annual_NegativeIncome_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Sub() TdsCalculator.Annual(-1D, Nothing, TdsTestRules.NewRegime(), Nothing))
    End Sub

    <Fact>
    Public Sub Monthly_SpreadsEqually_NearestRupee()
        Assert.Equal(2167D, TdsCalculator.Monthly(26000D, 0D, 12))
    End Sub

    <Fact>
    Public Sub Monthly_LastMonth_TakesBalance()
        Assert.Equal(2163D, TdsCalculator.Monthly(26000D, 23837D, 1))
    End Sub

    <Fact>
    Public Sub Monthly_AlreadyDeductedEnough_Zero()
        Assert.Equal(0D, TdsCalculator.Monthly(26000D, 26000D, 3))
    End Sub

    <Fact>
    Public Sub Monthly_PreviousEmployerTaxCounts()
        Assert.Equal(1000D, TdsCalculator.Monthly(26000D, 14000D, 12))
    End Sub

    <Fact>
    Public Sub Monthly_NoMonthsLeft_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Sub() TdsCalculator.Monthly(26000D, 0D, 0))
    End Sub
End Class
