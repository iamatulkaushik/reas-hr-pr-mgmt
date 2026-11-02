Imports Revolution.Core
Imports Revolution.Modules.Arrears
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class ArrearsMonthPreparerTests
    Private Shared Function Prepare(paidFacts As RunEmployeeFacts, dueFacts As RunEmployeeFacts) As OperationResult(Of ArrearsMonthInput)
        Return ArrearsMonthPreparer.Prepare(ArrearsTestData.Paid(paidFacts), dueFacts, ArrearsTestData.October(), ArrearsTestData.Policy(), ArrearsTestRates.Rates())
    End Function

    <Fact>
    Public Sub RevisedBasic_GivesTheDifference()
        Dim r As OperationResult(Of ArrearsMonthInput) = Prepare(ArrearsTestData.Facts(10000D), ArrearsTestData.Facts(12000D))
        Assert.True(r.Succeeded)
        Assert.Equal(New Date(2026, 10, 1), r.Value.WageMonth)
        Dim m As ArrearsMonthResult = ArrearsCalculator.CalculateMonth(r.Value)
        Assert.Equal(2000D, m.GrossDifference)
        Assert.Equal(2000D, m.EpfWages)
        Assert.Equal(240D, m.EmployeeEpf)
        Assert.Equal(0D, m.EsiEmployee)
    End Sub

    <Fact>
    Public Sub NotAnEpfMember_NoEpfOnTheDifference()
        Dim r As OperationResult(Of ArrearsMonthInput) = Prepare(ArrearsTestData.Facts(10000D, epfMember:=False), ArrearsTestData.Facts(12000D, epfMember:=False))
        Dim m As ArrearsMonthResult = ArrearsCalculator.CalculateMonth(r.Value)
        Assert.Equal(2000D, m.GrossDifference)
        Assert.Equal(0D, m.EpfWages)
        Assert.Equal(0D, m.EmployeeEpf)
        Assert.Equal(0D, m.EmployerEpf)
    End Sub

    <Fact>
    Public Sub NothingChanged_AllZero()
        Dim r As OperationResult(Of ArrearsMonthInput) = Prepare(ArrearsTestData.Facts(10000D), ArrearsTestData.Facts(10000D))
        Dim m As ArrearsMonthResult = ArrearsCalculator.CalculateMonth(r.Value)
        Assert.Equal(0D, m.GrossDifference)
        Assert.Equal(0D, m.EmployeeEpf)
        Assert.Empty(m.Lines)
    End Sub

    <Fact>
    Public Sub HeadRemovedNow_ShowsAsANegativeLine()
        Dim paidSalary As New List(Of SalaryLineView) From {New SalaryLineView(1, "BASIC", "Basic", True, False, True, 10000D), New SalaryLineView(5, "CONV", "Conveyance", False, False, False, 800D)}
        Dim paidFacts As New RunEmployeeFacts(101, 1, "E101", False, 1, Nothing, True, False, False, False, False, 31D, 0D, 8D, 0, 0D, 0D, paidSalary, Nothing)
        Dim r As OperationResult(Of ArrearsMonthInput) = Prepare(paidFacts, ArrearsTestData.Facts(10000D))
        Dim m As ArrearsMonthResult = ArrearsCalculator.CalculateMonth(r.Value)
        Assert.Equal(-800D, m.GrossDifference)
        Assert.Equal(-800D, m.Lines.Single(Function(l As EarnedLine) l.Code = "C5").Amount)
    End Sub

    <Fact>
    Public Sub DifferentEmployees_Refused()
        Dim r As OperationResult(Of ArrearsMonthInput) = Prepare(ArrearsTestData.Facts(10000D, 101), ArrearsTestData.Facts(12000D, 102))
        Assert.False(r.Succeeded)
    End Sub

    <Fact>
    Public Sub NoSalaryNow_ReturnsTheCalculatorsReason()
        Dim noSalary As New RunEmployeeFacts(101, 1, "E101", False, 1, Nothing, True, False, False, False, False, 31D, 0D, 8D, 0, 0D, 0D, New List(Of SalaryLineView)(), Nothing)
        Dim r As OperationResult(Of ArrearsMonthInput) = Prepare(ArrearsTestData.Facts(10000D), noSalary)
        Assert.False(r.Succeeded)
        Assert.Contains("no salary", r.Message, StringComparison.OrdinalIgnoreCase)
    End Sub
End Class
