Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>
''' A ten-year employee who resigned on 31 March 2025: gratuity 173,077 (30,000 wage), bonus 6,997 (12 months at 15,000 cut to 7,000, 8.33%), leave 12 days at 1,000,
''' notice 19 days short at 1,000, loan 5,000, last salary 25,000. Earnings 217,074, deductions 24,000, net 193,074.
''' </summary>
Public Class FnfPlannerTests
    Private Shared Function Facts() As FnfFacts
        Return New FnfFacts With {
            .EmployeeId = 101, .JoinDate = New Date(2015, 4, 1), .LastWorkingDay = New Date(2025, 3, 31), .LastSalaryNet = 25000D, .DailyWageGross = 1000D,
            .NoticeApplies = True, .NoticeDate = New Date(2025, 3, 21), .NoticePeriodDays = 30,
            .GratuityCovered = True, .GratuityWage = 30000D, .GratuityRule = New GratuityRule(New Date(2020, 4, 1), 5, 1, 15D, 26D, 6, 2000000D, True, Nothing, "test"),
            .BonusRule = New BonusRule(New Date(2020, 4, 1), 21000D, 7000D, 8.33D, 20D, 30, True, Nothing, "test"),
            .BonusLastMonthlyWage = 15000D, .BonusMonthlyWages = Enumerable.Repeat(15000D, 12).ToList(), .BonusDaysWorked = 365, .BonusPct = 8.33D,
            .LeaveEncashable = True, .LeaveBalanceDays = 12D, .LoanOutstanding = 5000D}
    End Function

    Private Shared Function Kinds(plan As FnfPlan) As List(Of FnfComponentKind)
        Return plan.Lines.Select(Function(l) l.Kind).ToList()
    End Function

    <Fact>
    Public Sub FullSettlement_AmountsAndTotals()
        Dim r = FnfPlanner.Plan(Facts())
        Assert.True(r.Succeeded)
        Dim plan As FnfPlan = r.Value
        Assert.Equal(6, plan.Lines.Count)
        Assert.Equal(217074D, plan.Totals.Earnings)
        Assert.Equal(24000D, plan.Totals.Deductions)
        Assert.Equal(193074D, plan.Totals.Net)
        Assert.Equal(0, plan.Notes.Count)
        Assert.Equal(173077D, plan.Lines.Single(Function(l) l.Kind = FnfComponentKind.Gratuity).Amount)
        Assert.Equal(6997D, plan.Lines.Single(Function(l) l.Kind = FnfComponentKind.Bonus).Amount)
        Assert.Equal(19000D, plan.Lines.Single(Function(l) l.Kind = FnfComponentKind.NoticePayRecovery).Amount)
    End Sub

    <Fact>
    Public Sub DueWindow_IsFirstToSeventhOfNextMonth()
        Dim w As RunPeriod = FnfPlanner.Plan(Facts()).Value.DueWindow
        Assert.Equal(New Date(2025, 4, 1), w.PeriodFrom)
        Assert.Equal(New Date(2025, 4, 7), w.PeriodTo)
    End Sub

    <Fact>
    Public Sub NotResigned_NoNoticeRecovery_WithANote()
        Dim f As FnfFacts = Facts()
        f.NoticeApplies = False
        Dim plan As FnfPlan = FnfPlanner.Plan(f).Value
        Assert.False(Kinds(plan).Contains(FnfComponentKind.NoticePayRecovery))
        Assert.Equal(1, plan.Notes.Count)
    End Sub

    <Fact>
    Public Sub FullNoticeServed_NoRecoveryLine()
        Dim f As FnfFacts = Facts()
        f.NoticeDate = New Date(2025, 3, 1)
        Assert.False(Kinds(FnfPlanner.Plan(f).Value).Contains(FnfComponentKind.NoticePayRecovery))
    End Sub

    <Fact>
    Public Sub WageAboveBonusCeiling_NoBonus_WithTheReason()
        Dim f As FnfFacts = Facts()
        f.BonusLastMonthlyWage = 22000D
        Dim plan As FnfPlan = FnfPlanner.Plan(f).Value
        Assert.False(Kinds(plan).Contains(FnfComponentKind.Bonus))
        Assert.True(plan.Notes.Single().StartsWith("Bonus not paid:", StringComparison.Ordinal))
    End Sub

    <Fact>
    Public Sub ContractEmployee_NoGratuity()
        Dim f As FnfFacts = Facts()
        f.GratuityCovered = False
        Dim plan As FnfPlan = FnfPlanner.Plan(f).Value
        Assert.False(Kinds(plan).Contains(FnfComponentKind.Gratuity))
        Assert.Equal("Gratuity does not apply to this employment type.", plan.Notes.Single())
    End Sub

    <Fact>
    Public Sub UnderMinimumService_NoGratuity_WithTheReason()
        Dim f As FnfFacts = Facts()
        f.JoinDate = New Date(2023, 4, 1)
        Dim plan As FnfPlan = FnfPlanner.Plan(f).Value
        Assert.False(Kinds(plan).Contains(FnfComponentKind.Gratuity))
        Assert.True(plan.Notes.Single().StartsWith("Gratuity not paid:", StringComparison.Ordinal))
    End Sub

    <Fact>
    Public Sub DeathOrDisablement_WaivesTheMinimum()
        Dim f As FnfFacts = Facts()
        f.JoinDate = New Date(2023, 4, 1)
        f.WaiveGratuityMinimum = True
        Assert.True(Kinds(FnfPlanner.Plan(f).Value).Contains(FnfComponentKind.Gratuity))
    End Sub

    <Fact>
    Public Sub NoRules_NoBonusAndNoGratuity_WithNotes()
        Dim f As FnfFacts = Facts()
        f.BonusRule = Nothing
        f.GratuityRule = Nothing
        Dim plan As FnfPlan = FnfPlanner.Plan(f).Value
        Assert.Equal(2, plan.Notes.Count)
        Assert.Equal(4, plan.Lines.Count)
    End Sub

    <Fact>
    Public Sub LeaveNotEncashable_NoLeaveLine()
        Dim f As FnfFacts = Facts()
        f.LeaveEncashable = False
        Assert.False(Kinds(FnfPlanner.Plan(f).Value).Contains(FnfComponentKind.LeaveEncashment))
    End Sub

    <Fact>
    Public Sub TaxRefundAndArrearsRecovery_FlowIntoTheLines()
        Dim f As FnfFacts = Facts()
        f.TdsAdjustment = -1500D
        f.ArrearsRecovery = 400D
        Dim plan As FnfPlan = FnfPlanner.Plan(f).Value
        Assert.Equal(218574D, plan.Totals.Earnings)
        Assert.Equal(24400D, plan.Totals.Deductions)
    End Sub

    <Fact>
    Public Sub TaxShortfall_IsADeduction()
        Dim f As FnfFacts = Facts()
        f.TdsAdjustment = 700D
        Assert.Equal(24700D, FnfPlanner.Plan(f).Value.Totals.Deductions)
    End Sub

    <Fact>
    Public Sub BadFacts_FailWithAMessage()
        Dim early As FnfFacts = Facts()
        early.LastWorkingDay = New Date(2015, 3, 1)
        Assert.False(FnfPlanner.Plan(early).Succeeded)
        Dim pct As FnfFacts = Facts()
        pct.BonusPct = 5D
        Assert.False(FnfPlanner.Plan(pct).Succeeded)
        Dim negative As FnfFacts = Facts()
        negative.LoanOutstanding = -1D
        Assert.False(FnfPlanner.Plan(negative).Succeeded)
    End Sub
End Class
