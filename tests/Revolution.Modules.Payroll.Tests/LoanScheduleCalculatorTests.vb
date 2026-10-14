Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Known answers by hand for flat interest. Reducing balance is checked by its rules: standard EMI, principal adds up, last installment clears.</summary>
Public Class LoanScheduleCalculatorTests
    <Fact>
    Public Sub Flat_EvenSplit()
        Dim s As IReadOnlyList(Of LoanInstallmentPlan) = LoanScheduleCalculator.Build(12000D, 10D, 12, InterestMethod.Flat, 202611)
        Assert.Equal(12, s.Count)
        Assert.Equal(1000D, s(0).PrincipalPart)
        Assert.Equal(100D, s(0).InterestPart)
        Assert.Equal(1100D, s(11).Amount)
    End Sub

    <Fact>
    Public Sub Flat_LastInstallmentTakesRemainder()
        Dim s As IReadOnlyList(Of LoanInstallmentPlan) = LoanScheduleCalculator.Build(10000D, 12D, 3, InterestMethod.Flat, 202611)
        Assert.Equal(3333D, s(0).PrincipalPart)
        Assert.Equal(3334D, s(2).PrincipalPart)
        Assert.Equal(100D, s(0).InterestPart)
        Assert.Equal(10000D, s.Sum(Function(x) x.PrincipalPart))
        Assert.Equal(300D, s.Sum(Function(x) x.InterestPart))
    End Sub

    <Fact>
    Public Sub NoInterest_EqualPrincipal()
        Dim s As IReadOnlyList(Of LoanInstallmentPlan) = LoanScheduleCalculator.Build(12000D, 0D, 12, InterestMethod.Reducing, 202611)
        Assert.Equal(12, s.Count)
        Assert.Equal(1000D, s(0).Amount)
        Assert.Equal(0D, s.Sum(Function(x) x.InterestPart))
    End Sub

    <Fact>
    Public Sub Reducing_StandardEmi()
        Dim s As IReadOnlyList(Of LoanInstallmentPlan) = LoanScheduleCalculator.Build(100000D, 12D, 12, InterestMethod.Reducing, 202611)
        Assert.Equal(12, s.Count)
        Assert.Equal(8885D, s(0).Amount)
        Assert.Equal(1000D, s(0).InterestPart)
        Assert.Equal(100000D, s.Sum(Function(x) x.PrincipalPart))
        Assert.True(Math.Abs(s(11).Amount - 8885D) <= 12D)
    End Sub

    <Fact>
    Public Sub Reducing_InterestFallsEachMonth()
        Dim s As IReadOnlyList(Of LoanInstallmentPlan) = LoanScheduleCalculator.Build(100000D, 12D, 12, InterestMethod.Reducing, 202611)
        Assert.True(s(1).InterestPart < s(0).InterestPart)
        Assert.True(s(11).InterestPart < s(5).InterestPart)
    End Sub

    <Fact>
    Public Sub DueMonths_RollOverYear()
        Dim s As IReadOnlyList(Of LoanInstallmentPlan) = LoanScheduleCalculator.Build(3000D, 0D, 3, InterestMethod.Flat, 202611)
        Assert.Equal(202611, s(0).DueMonthKey)
        Assert.Equal(202612, s(1).DueMonthKey)
        Assert.Equal(202701, s(2).DueMonthKey)
    End Sub

    <Fact>
    Public Sub SingleInstallment_ClearsLoan()
        Dim s As IReadOnlyList(Of LoanInstallmentPlan) = LoanScheduleCalculator.Build(5000D, 12D, 1, InterestMethod.Reducing, 202611)
        Assert.Equal(1, s.Count)
        Assert.Equal(5000D, s(0).PrincipalPart)
    End Sub

    <Fact>
    Public Sub FractionalPrincipal_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() LoanScheduleCalculator.Build(1000.5D, 0D, 3, InterestMethod.Flat, 202611))
    End Sub

    <Fact>
    Public Sub BadMonth_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() LoanScheduleCalculator.Build(1000D, 0D, 3, InterestMethod.Flat, 202613))
    End Sub

    <Fact>
    Public Sub TooManyInstallments_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() LoanScheduleCalculator.Build(1000D, 0D, 361, InterestMethod.Flat, 202611))
    End Sub
End Class
