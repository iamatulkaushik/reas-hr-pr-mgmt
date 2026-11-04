Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class BonusTaxPlannerTests
    Private Shared ReadOnly Period As New RunPeriod(New Date(2026, 11, 1), New Date(2026, 11, 30))
    Private ReadOnly _rates As New FakeTdsRates()
    Private ReadOnly _projection As TdsProjectionService

    Public Sub New()
        _projection = New TdsProjectionService(_rates)
    End Sub

    Private Shared Function Facts(monthly As Decimal, fromMonth As Integer, toMonth As Integer) As EmployeeTaxFacts
        Dim months As New List(Of TdsMonthFacts)()
        For month As Integer = fromMonth To toMonth
            months.Add(New TdsMonthFacts(202600 + month, monthly / 2D, monthly / 5D, monthly))
        Next
        Return New EmployeeTaxFacts(7, New Date(1990, 5, 5), months, 0D, Nothing)
    End Function

    Private Function Plan(bonus As Decimal, tax As EmployeeTaxFacts) As OperationResult(Of Decimal)
        Return BonusTaxPlanner.Plan(Period, bonus, tax, AddressOf _projection.Project)
    End Function

    <Fact>
    Public Sub HighEarner_PaysTaxOnTheBonus_ButLessThanTheBonus()
        Dim r = Plan(50000D, Facts(200000D, 4, 10))
        Assert.True(r.Succeeded)
        Assert.True(r.Value > 0D)
        Assert.True(r.Value < 50000D)
    End Sub

    <Fact>
    Public Sub HigherBonus_MeansMoreTax()
        Dim small = Plan(10000D, Facts(200000D, 4, 10))
        Dim large = Plan(100000D, Facts(200000D, 4, 10))
        Assert.True(large.Value > small.Value)
    End Sub

    <Fact>
    Public Sub LowEarner_UnderTheRebate_PaysNothing()
        Dim r = Plan(7000D, Facts(20000D, 4, 10))
        Assert.True(r.Succeeded)
        Assert.Equal(0D, r.Value)
    End Sub

    <Fact>
    Public Sub NoBonus_NoTax()
        Assert.Equal(0D, Plan(0D, Facts(200000D, 4, 10)).Value)
    End Sub

    <Fact>
    Public Sub NoLockedPayInTheYear_StillWorks_AndTheBonusAloneIsBelowTheRebate()
        Dim none As New EmployeeTaxFacts(7, New Date(1990, 5, 5), New List(Of TdsMonthFacts)(), 0D, Nothing)
        Dim r = Plan(7000D, none)
        Assert.True(r.Succeeded)
        Assert.Equal(0D, r.Value)
    End Sub

    <Fact>
    Public Sub TheLatestLockedMonthIsRepeatedForTheMonthsAfter_NotTheBonus()
        Dim withMonths = Plan(100000D, Facts(200000D, 4, 10))
        Dim withoutRepeat = Plan(100000D, Facts(200000D, 4, 5))
        Assert.True(withMonths.Value > 0D)
        Assert.True(withoutRepeat.Value >= 0D)
    End Sub

    <Fact>
    Public Sub RuleFailure_IsReturned_AndNothingIsGuessed()
        _rates.RuleFailure = "Tax rule is not signed off."
        Dim r = Plan(50000D, Facts(200000D, 4, 10))
        Assert.False(r.Succeeded)
        Assert.Equal("Tax rule is not signed off.", r.Message)
    End Sub
End Class
