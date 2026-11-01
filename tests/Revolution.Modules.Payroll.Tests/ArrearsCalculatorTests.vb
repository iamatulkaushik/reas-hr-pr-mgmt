Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Known answers for arrears: difference of earnings and contributions per wage month. Rates are fixtures; the CA signs the expected values.</summary>
Public Class ArrearsCalculatorTests
    Private Shared Function Wage(basic As Decimal) As WageBreakdown
        Dim lines As New List(Of EarnedLine) From {New EarnedLine("BASIC", basic)}
        Return New WageBreakdown(lines, 0D, basic, basic, basic)
    End Function

    Private Shared Function Rates() As RunRates
        Return New RunRates(StatutoryTestRates.Epf(), StatutoryTestRates.Esi(), StatutoryTestRates.CodeWage(), New Dictionary(Of Integer, PtRule)())
    End Function

    Private Shared Function Month(year As Integer, monthNo As Integer, paid As Decimal, due As Decimal) As ArrearsMonthInput
        Return New ArrearsMonthInput(New Date(year, monthNo, 1), Wage(paid), Wage(due), Rates(), 400D, False)
    End Function

    <Fact>
    Public Sub BelowCeiling_ContributionsOnDifference()
        Dim m As ArrearsMonthResult = ArrearsCalculator.CalculateMonth(Month(2026, 5, 10000D, 12000D))
        Assert.Equal(2000D, m.GrossDifference)
        Assert.Equal(2000D, m.EpfWages)
        Assert.Equal(240D, m.EmployeeEpf)
        Assert.Equal(167D, m.EmployerEps)
        Assert.Equal(73D, m.EmployerEpf)
        Assert.Equal(15D, m.EsiEmployee)
        Assert.Equal(65D, m.EsiEmployer)
    End Sub

    <Fact>
    Public Sub CrossingCeiling_OnlyRoomUnderCeilingCounts()
        Dim m As ArrearsMonthResult = ArrearsCalculator.CalculateMonth(Month(2026, 6, 14000D, 18000D))
        Assert.Equal(4000D, m.GrossDifference)
        Assert.Equal(1000D, m.EpfWages)
        Assert.Equal(120D, m.EmployeeEpf)
    End Sub

    <Fact>
    Public Sub Overpaid_GivesNegativeDifference()
        Dim m As ArrearsMonthResult = ArrearsCalculator.CalculateMonth(Month(2026, 7, 12000D, 10000D))
        Assert.Equal(-2000D, m.GrossDifference)
        Assert.Equal(-240D, m.EmployeeEpf)
    End Sub

    <Fact>
    Public Sub Months_AreOrderedAndTotalled()
        Dim r As ArrearsResult = ArrearsCalculator.Calculate(New List(Of ArrearsMonthInput) From {
            Month(2026, 6, 10000D, 12000D), Month(2026, 5, 10000D, 12000D)})
        Assert.Equal(5, r.Months(0).WageMonth.Month)
        Assert.Equal(4000D, r.GrossTotal)
        Assert.Equal(510D, r.EmployeeDeductionTotal)
        Assert.Equal(3490D, r.NetBeforeTax)
    End Sub

    <Fact>
    Public Sub SameMonthTwice_Refused()
        Assert.Throws(Of ArgumentException)(Function() ArrearsCalculator.Calculate(New List(Of ArrearsMonthInput) From {
            Month(2026, 5, 1D, 2D), Month(2026, 5, 1D, 3D)}))
    End Sub

    <Fact>
    Public Sub NoMonths_Refused()
        Assert.Throws(Of ArgumentException)(Function() ArrearsCalculator.Calculate(New List(Of ArrearsMonthInput)()))
    End Sub
End Class
