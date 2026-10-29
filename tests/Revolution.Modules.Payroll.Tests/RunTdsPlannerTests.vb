Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Run month October 2026 (key 202610), financial year 2026. The fixture pay is 16,000: basic 10,000, DA 2,000, HRA 4,000.</summary>
Public Class RunTdsPlannerTests
    Private Shared Function Salary(Optional scale As Decimal = 1D) As IReadOnlyList(Of SalaryLineView)
        Return New List(Of SalaryLineView) From {
            New SalaryLineView(1, "BASIC", "Basic", True, False, True, 10000D * scale),
            New SalaryLineView(2, "DA", "Dearness allowance", False, True, True, 2000D * scale),
            New SalaryLineView(3, "HRA", "House rent allowance", False, False, False, 4000D * scale, True)}
    End Function

    Private Shared Function Wage() As WageBreakdown
        Dim seen As WageBreakdown = Nothing
        Dim hook As Func(Of WageBreakdown, OperationResult(Of Decimal)) = Function(w)
                                                                               seen = w
                                                                               Return OperationResult(Of Decimal).Ok(0D)
                                                                           End Function
        PayrollRunCalculator.Calculate(RunTestData.Facts(), RunTestData.October(), RunTestData.Policy(), RunTestData.Rates(), hook)
        Return seen
    End Function

    Private Shared Function Locked(ParamArray keys As Integer()) As List(Of TdsMonthFacts)
        Return keys.Select(Function(k) New TdsMonthFacts(k, 11000D, 3500D, 15000D)).ToList()
    End Function

    Private Shared Function Facts(months As List(Of TdsMonthFacts), deducted As Decimal) As EmployeeTaxFacts
        Return New EmployeeTaxFacts(101, New Date(1990, 5, 5), months, deducted, Nothing)
    End Function

    Private Shared Function Result(tds As Decimal) As OperationResult(Of TdsProjectionResult)
        Dim annual As New TdsAnnualResult(0D, 0D, 0D, 0D, 0D, 0D, 0D, 0D)
        Return OperationResult(Of TdsProjectionResult).Ok(New TdsProjectionResult(TdsRegime.New, True, 0D, 0D, annual, 0D, 7, tds))
    End Function

    Private Shared Function Plan(tax As EmployeeTaxFacts, ByRef captured As TdsProjectionInput, Optional scale As Decimal = 1D) As OperationResult(Of Decimal)
        Dim got As TdsProjectionInput = Nothing
        Dim planResult As OperationResult(Of Decimal) = RunTdsPlanner.Plan(RunTestData.October(), Wage(), Salary(scale), tax,
                                                                           Function(i)
                                                                               got = i
                                                                               Return Result(1234D)
                                                                           End Function)
        captured = got
        Return planResult
    End Function

    <Fact>
    Public Sub LockedMonthsStay_ThisMonthAndLaterRepeatThisPay()
        Dim input As TdsProjectionInput = Nothing
        Dim r = Plan(Facts(Locked(202604, 202605, 202606, 202607, 202608, 202609), 6000D), input)
        Assert.True(r.Succeeded)
        Assert.Equal(1234D, r.Value)
        Assert.Equal(12, input.Months.Count)
        Assert.Equal(202604, input.Months(0).MonthKey)
        Assert.Equal(15000D, input.Months(5).TaxableSalary)
        Assert.Equal(202610, input.Months(6).MonthKey)
        Assert.Equal(16000D, input.Months(6).TaxableSalary)
        Assert.Equal(12000D, input.Months(6).BasicPlusDa)
        Assert.Equal(4000D, input.Months(6).HraReceived)
        Assert.Equal(16000D, input.Months(11).TaxableSalary)
        Assert.Equal(202703, input.Months(11).MonthKey)
    End Sub

    <Fact>
    Public Sub PassesTheFactsThrough()
        Dim input As TdsProjectionInput = Nothing
        Plan(Facts(Locked(202604), 6000D), input)
        Assert.Equal(6000D, input.TaxDeductedBefore)
        Assert.Equal(202610, input.RunMonthKey)
        Assert.Equal(2026, input.FinancialYear)
        Assert.Equal(101, input.EmployeeId)
        Assert.Equal(New Date(1990, 5, 5), input.DateOfBirth)
    End Sub

    <Fact>
    Public Sub MonthsBeforeTheFirstLockedPay_AreZero()
        Dim input As TdsProjectionInput = Nothing
        Plan(Facts(New List(Of TdsMonthFacts)(), 0D), input)
        Assert.Equal(0D, input.Months(0).TaxableSalary)
        Assert.Equal(0D, input.Months(5).TaxableSalary)
        Assert.Equal(16000D, input.Months(6).TaxableSalary)
    End Sub

    <Fact>
    Public Sub LockedArrearsInTheSameMonth_AddToThisMonth()
        Dim input As TdsProjectionInput = Nothing
        Plan(Facts(Locked(202610), 0D), input)
        Assert.Equal(31000D, input.Months(6).TaxableSalary)
        Assert.Equal(23000D, input.Months(6).BasicPlusDa)
        Assert.Equal(7500D, input.Months(6).HraReceived)
        Assert.Equal(16000D, input.Months(7).TaxableSalary)
    End Sub

    <Fact>
    Public Sub PartMonth_ScalesBasicDaAndHra()
        Dim input As TdsProjectionInput = Nothing
        Plan(Facts(New List(Of TdsMonthFacts)(), 0D), input, 2D)
        Assert.Equal(16000D, input.Months(6).TaxableSalary)
        Assert.Equal(12000D, input.Months(6).BasicPlusDa)
        Assert.Equal(4000D, input.Months(6).HraReceived)
    End Sub

    <Fact>
    Public Sub ProjectionFailure_IsReturned()
        Dim r = RunTdsPlanner.Plan(RunTestData.October(), Wage(), Salary(), Facts(New List(Of TdsMonthFacts)(), 0D),
                                   Function(i) OperationResult(Of TdsProjectionResult).Fail("No tax slab row is in force."))
        Assert.False(r.Succeeded)
        Assert.Equal("No tax slab row is in force.", r.Message)
    End Sub
End Class
