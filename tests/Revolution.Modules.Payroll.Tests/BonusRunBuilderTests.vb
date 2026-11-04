Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Fixture rule: eligible up to 21,000 a month, calculated on at most 7,000 a month, 8.33% to 20%, at least 30 days worked. Twelve months at 15,000 give a base of 84,000.</summary>
Public Class BonusRunBuilderTests
    Private Shared Function Rule() As BonusRule
        Return New BonusRule(New Date(2020, 4, 1), 21000D, 7000D, 8.33D, 20D, 30, True, Nothing, "test")
    End Function

    Private Shared Function Build(source As FakeBonusSource, pct As Decimal, Optional taxFor As Func(Of Integer, Decimal, OperationResult(Of Decimal)) = Nothing) As BonusRunPlan
        Return BonusRunBuilder.Build(Rule(), New BonusFacts(source.Months, source.Paid, source.Flags), pct, StatutoryTestRates.Epf(), taxFor)
    End Function

    <Fact>
    Public Sub EligibleEmployee_GetsALine_WithNetEqualToTheBonus_AndNoDeductions()
        Dim source As New FakeBonusSource()
        source.Year(101, "E101", 15000D)
        Dim plan As BonusRunPlan = Build(source, 8.33D)
        Assert.Single(plan.Rows)
        Assert.Equal(84000D, plan.Rows(0).Result.WageBase)
        Dim line As RunLineResult = plan.Lines.Single()
        Assert.Equal(101, line.EmployeeId)
        Assert.Equal(6997D, line.GrossEarnings)
        Assert.Equal(6997D, line.NetPay)
        Assert.Equal(0D, line.TotalDeductions)
        Assert.Equal(0D, line.EpfEmployee)
        Assert.Equal(0D, line.EsiEmployee)
        Assert.Equal(0D, line.PaidDays)
        Assert.Empty(line.Earnings)
        Assert.Equal(6997D, plan.TotalAmount)
    End Sub

    <Fact>
    Public Sub MaximumPct_PaysTwentyPercentOfTheBase()
        Dim source As New FakeBonusSource()
        source.Year(101, "E101", 15000D)
        Assert.Equal(16800D, Build(source, 20D).Lines.Single().NetPay)
    End Sub

    <Fact>
    Public Sub WageAboveTheEligibilityCeiling_GetsNoLine_AndTheReason()
        Dim source As New FakeBonusSource()
        source.Year(101, "E101", 25000D)
        Dim plan As BonusRunPlan = Build(source, 8.33D)
        Assert.Empty(plan.Lines)
        Assert.False(plan.Rows.Single().Result.IsEligible)
        Assert.Contains("ceiling", plan.Rows.Single().Result.Reason, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub TheLatestMonthsWage_DecidesEligibility()
        Dim source As New FakeBonusSource()
        source.Year(101, "E101", 15000D)
        source.Months.RemoveAt(source.Months.Count - 1)
        source.Months.Add(New BonusMonthFact(101, "E101", 1, 202603, 25000D, 30D))
        Dim plan As BonusRunPlan = Build(source, 8.33D)
        Assert.Empty(plan.Lines)
        Assert.Equal(25000D, plan.Rows.Single().LastMonthlyWage)
    End Sub

    <Fact>
    Public Sub FewerThanTheMinimumDays_GetsNoLine()
        Dim source As New FakeBonusSource()
        source.Year(101, "E101", 15000D, paidDays:=2D)
        Dim plan As BonusRunPlan = Build(source, 8.33D)
        Assert.Empty(plan.Lines)
        Assert.Equal(24, plan.Rows.Single().DaysWorked)
        Assert.Contains("days", plan.Rows.Single().Result.Reason, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub PartDays_AreNotCountedTowardsTheMinimum()
        Dim source As New FakeBonusSource()
        source.Year(101, "E101", 15000D, paidDays:=2.49D)
        Dim plan As BonusRunPlan = Build(source, 8.33D)
        Assert.Equal(29, plan.Rows.Single().DaysWorked)
        Assert.Empty(plan.Lines)
    End Sub

    <Fact>
    Public Sub AlreadyPaid_IsTakenOff()
        Dim source As New FakeBonusSource()
        source.Year(101, "E101", 15000D)
        source.Paid(101) = 1000D
        Dim plan As BonusRunPlan = Build(source, 8.33D)
        Assert.Equal(5997D, plan.Lines.Single().NetPay)
        Assert.Equal(1000D, plan.Rows.Single().Result.AlreadyPaid)
    End Sub

    <Fact>
    Public Sub AlreadyPaidInFull_HasARowButNoLine()
        Dim source As New FakeBonusSource()
        source.Year(101, "E101", 15000D)
        source.Paid(101) = 7000D
        Dim plan As BonusRunPlan = Build(source, 8.33D)
        Assert.Empty(plan.Lines)
        Assert.Equal(0D, plan.Rows.Single().Amount)
    End Sub

    <Fact>
    Public Sub Employees_AreInIdOrder_AndTheFactoryIsTheLatestMonths()
        Dim source As New FakeBonusSource()
        source.Year(202, "E202", 15000D, factoryId:=2)
        source.Year(101, "E101", 15000D, factoryId:=1)
        source.Months.RemoveAt(source.Months.Count - 1)
        source.Months.Add(New BonusMonthFact(101, "E101", 7, 202603, 15000D, 30D))
        Dim plan As BonusRunPlan = Build(source, 8.33D)
        Assert.Equal(New List(Of Integer) From {101, 202}, plan.Rows.Select(Function(r) r.EmployeeId).ToList())
        Assert.Equal(7, plan.Lines.Single(Function(l) l.EmployeeId = 101).FactoryId)
        Assert.Equal(2, plan.Lines.Single(Function(l) l.EmployeeId = 202).FactoryId)
    End Sub

    <Fact>
    Public Sub PlanCarriesThePercentageAndTheRulesRange()
        Dim source As New FakeBonusSource()
        source.Year(101, "E101", 15000D)
        Dim plan As BonusRunPlan = Build(source, 10D)
        Assert.Equal(10D, plan.Pct)
        Assert.Equal(8.33D, plan.MinPct)
        Assert.Equal(20D, plan.MaxPct)
    End Sub

    <Fact>
    Public Sub NonMember_HasNoProvidentFund_AndNetEqualsTheBonus()
        Dim source As New FakeBonusSource()
        source.Year(101, "E101", 15000D)
        Dim line As RunLineResult = Build(source, 8.33D).Lines.Single()
        Assert.Equal(0D, line.EpfEmployee)
        Assert.Equal(0D, line.EpfWage)
        Assert.Equal(6997D, line.NetPay)
    End Sub

    <Fact>
    Public Sub EpfMember_PaysProvidentFundOnTheBonusAsItsOwnWage()
        Dim source As New FakeBonusSource()
        source.Year(101, "E101", 15000D)
        source.Flags(101) = New BonusEmployeeFlags(101, True, False, False)
        Dim expected As EpfResult = EpfCalculator.Calculate(6997D, StatutoryTestRates.Epf(), False, False)
        Dim plan As BonusRunPlan = Build(source, 8.33D)
        Dim line As RunLineResult = plan.Lines.Single()
        Assert.True(expected.EmployeeShare > 0D)
        Assert.Equal(6997D, line.EpfWage)
        Assert.Equal(expected.EmployeeShare, line.EpfEmployee)
        Assert.Equal(expected.EmployerEpf, line.EpfEmployerEpf)
        Assert.Equal(expected.EmployerEps, line.EpfEmployerEps)
        Assert.Equal(6997D - expected.EmployeeShare, line.NetPay)
        Assert.Equal(expected.EmployeeShare, line.TotalDeductions)
        Assert.Equal(expected.EmployeeShare, plan.Rows.Single().EpfEmployee)
        Assert.Equal(0D, line.EsiEmployee)
        Assert.Equal(0D, line.ProfessionalTax)
    End Sub

    <Fact>
    Public Sub EpfIsCappedAtTheWageCeiling_OnTheBonus()
        Dim source As New FakeBonusSource()
        source.Year(101, "E101", 15000D)
        source.Flags(101) = New BonusEmployeeFlags(101, True, False, False)
        Dim line As RunLineResult = Build(source, 20D).Lines.Single()
        Assert.Equal(1800D, line.EpfEmployee)
        Assert.Equal(16800D - 1800D, line.NetPay)
    End Sub

    <Fact>
    Public Sub IncomeTax_IsTakenFromTheBonus_AfterProvidentFund()
        Dim source As New FakeBonusSource()
        source.Year(101, "E101", 15000D)
        source.Flags(101) = New BonusEmployeeFlags(101, True, False, False)
        Dim pf As Decimal = EpfCalculator.Calculate(6997D, StatutoryTestRates.Epf(), False, False).EmployeeShare
        Dim plan As BonusRunPlan = Build(source, 8.33D, Function(id, amount) OperationResult(Of Decimal).Ok(500D))
        Dim line As RunLineResult = plan.Lines.Single()
        Assert.Equal(500D, line.TdsAmount)
        Assert.Equal(0D, line.TdsShortfall)
        Assert.Equal(6997D - pf - 500D, line.NetPay)
        Assert.Equal(pf + 500D, line.TotalDeductions)
        Assert.Equal(500D, plan.Rows.Single().Tds)
        Assert.Equal(line.NetPay, plan.Rows.Single().NetPay)
    End Sub

    <Fact>
    Public Sub TaxThatTheBonusCannotCover_IsReportedAsAShortfall_AndNetIsZero()
        Dim source As New FakeBonusSource()
        source.Year(101, "E101", 15000D)
        Dim line As RunLineResult = Build(source, 8.33D, Function(id, amount) OperationResult(Of Decimal).Ok(10000D)).Lines.Single()
        Assert.Equal(6997D, line.TdsAmount)
        Assert.Equal(3003D, line.TdsShortfall)
        Assert.Equal(0D, line.NetPay)
    End Sub

    <Fact>
    Public Sub TaxFailure_IsAProblem_NamedByEmployeeCode_AndTheEmployeeGetsNoLine()
        Dim source As New FakeBonusSource()
        source.Year(101, "E101", 15000D)
        source.Year(102, "E102", 15000D)
        Dim plan As BonusRunPlan = Build(source, 8.33D, Function(id, amount) If(id = 101, OperationResult(Of Decimal).Fail("No tax facts were found."), OperationResult(Of Decimal).Ok(0D)))
        Assert.Equal(New List(Of String) From {"E101: No tax facts were found."}, plan.Problems)
        Assert.Equal(102, plan.Lines.Single().EmployeeId)
        Assert.Equal(2, plan.Rows.Count)
    End Sub

    <Fact>
    Public Sub TaxIsNotAskedFor_AnEmployeeWithNothingToPay()
        Dim source As New FakeBonusSource()
        source.Year(101, "E101", 25000D)
        Dim asked As Integer
        Build(source, 8.33D, Function(id, amount)
                                 asked += 1
                                 Return OperationResult(Of Decimal).Ok(0D)
                             End Function)
        Assert.Equal(0, asked)
    End Sub
End Class
