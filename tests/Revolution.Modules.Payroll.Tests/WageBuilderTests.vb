Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Known answers by hand. Heads: basic 10,000, DA 2,000, HRA 4,000 (a wage exclusion). Overtime 2x. The CA signs the expected values.</summary>
Public Class WageBuilderTests
    Private Shared Function Heads() As IReadOnlyList(Of SalaryLineInput)
        Return New List(Of SalaryLineInput) From {
            New SalaryLineInput("BASIC", True, False, False, 10000D),
            New SalaryLineInput("DA", False, True, False, 2000D),
            New SalaryLineInput("HRA", False, False, False, 4000D)
        }
    End Function

    Private Shared Function Policy(Optional otEpf As Boolean = False, Optional otEsi As Boolean = False) As PayPolicy
        Return New PayPolicy(2D, otEpf, otEsi, 26D)
    End Function

    Private Shared Function Period(days As Integer, paid As Decimal, Optional otHours As Decimal = 0D) As PayPeriod
        Return New PayPeriod(days, paid, otHours, 8D)
    End Function

    <Fact>
    Public Sub FullMonth_CodeWage()
        Dim w As WageBreakdown = WageBuilder.Build(Heads(), False, Period(30, 30D), Policy(), StatutoryTestRates.CodeWage())
        Assert.Equal(16000D, w.GrossEarnings)
        Assert.Equal(12000D, w.EpfWage)
        Assert.Equal(12000D, w.EsiWage)
        Assert.Equal(0D, w.OvertimeAmount)
    End Sub

    <Fact>
    Public Sub FullMonth_OldRule_FlaggedWage()
        Dim w As WageBreakdown = WageBuilder.Build(Heads(), False, Period(30, 30D), Policy(), StatutoryTestRates.OldWage())
        Assert.Equal(12000D, w.EpfWage)
    End Sub

    <Fact>
    Public Sub HalfMonth_ProRata()
        Dim w As WageBreakdown = WageBuilder.Build(Heads(), False, Period(30, 15D), Policy(), StatutoryTestRates.CodeWage())
        Assert.Equal(5000D, w.Lines(0).Amount)
        Assert.Equal(1000D, w.Lines(1).Amount)
        Assert.Equal(2000D, w.Lines(2).Amount)
        Assert.Equal(8000D, w.GrossEarnings)
        Assert.Equal(6000D, w.EpfWage)
    End Sub

    <Fact>
    Public Sub EachHeadRoundedOnce()
        Dim w As WageBreakdown = WageBuilder.Build(Heads(), False, Period(31, 29D), Policy(), StatutoryTestRates.CodeWage())
        Assert.Equal(9355D, w.Lines(0).Amount)
        Assert.Equal(1871D, w.Lines(1).Amount)
        Assert.Equal(3742D, w.Lines(2).Amount)
        Assert.Equal(14968D, w.GrossEarnings)
    End Sub

    <Fact>
    Public Sub HalfDayCounts_AsHalf()
        Dim w As WageBreakdown = WageBuilder.Build(Heads(), False, Period(30, 29.5D), Policy(), StatutoryTestRates.CodeWage())
        Assert.Equal(9833D, w.Lines(0).Amount)
    End Sub

    <Fact>
    Public Sub Overtime_TwiceOrdinaryRate()
        Dim w As WageBreakdown = WageBuilder.Build(Heads(), False, Period(30, 30D, 10D), Policy(), StatutoryTestRates.CodeWage())
        Assert.Equal(1000D, w.OvertimeAmount)
        Assert.Equal(17000D, w.GrossEarnings)
        Assert.Equal(12000D, w.EpfWage)
        Assert.Equal(12000D, w.EsiWage)
    End Sub

    <Fact>
    Public Sub Overtime_InEpfWage_Option()
        Dim w As WageBreakdown = WageBuilder.Build(Heads(), False, Period(30, 30D, 10D), Policy(True, False), StatutoryTestRates.CodeWage())
        Assert.Equal(13000D, w.EpfWage)
        Assert.Equal(12000D, w.EsiWage)
    End Sub

    <Fact>
    Public Sub Overtime_InEsiWage_Option()
        Dim w As WageBreakdown = WageBuilder.Build(Heads(), False, Period(30, 30D, 10D), Policy(False, True), StatutoryTestRates.CodeWage())
        Assert.Equal(12000D, w.EpfWage)
        Assert.Equal(13000D, w.EsiWage)
    End Sub

    <Fact>
    Public Sub CodeWage_ExclusionsAboveHalf_WageIsHalf()
        Dim heads As New List(Of SalaryLineInput) From {
            New SalaryLineInput("BASIC", True, False, False, 5000D),
            New SalaryLineInput("HRA", False, False, False, 10000D)
        }
        Dim w As WageBreakdown = WageBuilder.Build(heads, False, Period(30, 30D), Policy(), StatutoryTestRates.CodeWage())
        Assert.Equal(7500D, w.EpfWage)
    End Sub

    <Fact>
    Public Sub OvertimeBase_IncludesMarkedHeads()
        Dim heads As New List(Of SalaryLineInput) From {
            New SalaryLineInput("BASIC", True, False, False, 9000D),
            New SalaryLineInput("CONV", False, False, True, 3000D)
        }
        Dim w As WageBreakdown = WageBuilder.Build(heads, False, Period(30, 30D, 10D), Policy(), StatutoryTestRates.CodeWage())
        Assert.Equal(1000D, w.OvertimeAmount)
    End Sub

    <Fact>
    Public Sub DailyWage_DividedByDivisor()
        Dim heads As New List(Of SalaryLineInput) From {New SalaryLineInput("BASIC", True, False, False, 7800D)}
        Dim w As WageBreakdown = WageBuilder.Build(heads, True, Period(30, 20D, 4D), Policy(), StatutoryTestRates.CodeWage())
        Assert.Equal(6000D, w.Lines(0).Amount)
        Assert.Equal(300D, w.OvertimeAmount)
        Assert.Equal(6300D, w.GrossEarnings)
    End Sub

    <Fact>
    Public Sub NoLines_AllZero()
        Dim w As WageBreakdown = WageBuilder.Build(New List(Of SalaryLineInput)(), False, Period(30, 30D), Policy(), StatutoryTestRates.CodeWage())
        Assert.Equal(0D, w.GrossEarnings)
    End Sub

    <Fact>
    Public Sub MonthlyPaidDaysAbovePeriod_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() WageBuilder.Build(Heads(), False, Period(30, 31D), Policy(), StatutoryTestRates.CodeWage()))
    End Sub

    <Fact>
    Public Sub NegativeAmount_Throws()
        Dim heads As New List(Of SalaryLineInput) From {New SalaryLineInput("BASIC", True, False, False, -1D)}
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() WageBuilder.Build(heads, False, Period(30, 30D), Policy(), StatutoryTestRates.CodeWage()))
    End Sub

    <Fact>
    Public Sub NullPolicy_Throws()
        Assert.Throws(Of ArgumentNullException)(Function() WageBuilder.Build(Heads(), False, Period(30, 30D), Nothing, StatutoryTestRates.CodeWage()))
    End Sub
End Class
