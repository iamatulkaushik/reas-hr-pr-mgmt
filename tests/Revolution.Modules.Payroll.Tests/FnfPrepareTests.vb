Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Friend NotInheritable Class FakeFnfFactsSource
    Implements IFnfFactsSource

    Public Facts As FnfSourceFacts

    Public Function Load(separationId As Integer) As FnfSourceFacts Implements IFnfFactsSource.Load
        Return Facts
    End Function
End Class

''' <summary>
''' The ten-year employee of FnfPlannerTests, now read through the service: salary 26,000 gross over a divisor of 26 is 1,000 a day, Basic plus DA 15,000 (so gratuity is 86,538), resigned on 31 March 2025 in the
''' March 2025 run (key 202503), whose net pay is 25,000. Bonus is on 12 months of Basic plus DA at 15,000 (11 locked and the final one).
''' </summary>
Public Class FnfPrepareTests
    Private ReadOnly _source As New FakeFnfFactsSource()
    Private ReadOnly _rates As New FakeRates()
    Private ReadOnly _leave As New FakeLeaveRepository()
    Private ReadOnly _tax As New FakeRunTaxSource()
    Private ReadOnly _plan As New FakePayEntitlements()
    Private ReadOnly _perms As New FakePayPermissions()
    Private ReadOnly _service As FnfService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _rates.GratuityRuleToReturn = New GratuityRule(New Date(2020, 4, 1), 5, 1, 15D, 26D, 6, 2000000D, True, Nothing, "test")
        _rates.BonusRuleToReturn = New BonusRule(New Date(2020, 4, 1), 21000D, 7000D, 8.33D, 20D, 30, True, Nothing, "test")
        _leave.Balances.Add(New LeaveBalanceRow(1, "EL", "Earned leave", True, 30D, 12D, 0D, 0D, 0D, 0D, 0D, 12D))
        _source.Facts = Facts()
        _service = New FnfService(New FakeFnfRepository(), _perms, _plan, New FakePayAudit(), New FakePaySession(), _source, _rates, _leave, _tax, New TdsProjectionService(New FakeTdsRates()))
    End Sub

    Private Shared Function Facts() As FnfSourceFacts
        Dim months As New List(Of FnfWorkedMonth)()
        For m As Integer = 0 To 10
            months.Add(New FnfWorkedMonth(If(m < 9, 202404 + m, 202501 + m - 9), 15000D, 30D))
        Next
        Return New FnfSourceFacts With {
            .EmployeeId = 101, .JoinDate = New Date(2015, 4, 1), .DateOfBirth = New Date(1985, 1, 1), .EmploymentTypeId = 1, .ReasonId = 1,
            .NoticeDate = New Date(2025, 3, 21), .LastWorkingDay = New Date(2025, 3, 31), .NoticePeriodDays = 30, .DailyWageDivisor = 26D,
            .MonthlyGross = 26000D, .MonthlyBasicPlusDa = 15000D, .LoanOutstanding = 5000D, .BonusMonths = months,
            .FinalRunId = 9, .FinalMonthKey = 202503, .FinalNetPay = 25000D, .FinalGross = 26000D, .FinalBasicPlusDa = 15000D, .FinalHra = 0D, .FinalPaidDays = 30D}
    End Function

    Private Function Plan(Optional options As FnfPrepareOptions = Nothing) As OperationResult(Of FnfPlan)
        Return _service.Prepare(5, options)
    End Function

    <Fact>
    Public Sub Resigned_TenYears_BuildsTheFullSettlement()
        Dim r = Plan()
        Assert.True(r.Succeeded)
        Dim p As FnfPlan = r.Value
        Assert.Equal(86538D, p.Lines.Single(Function(l) l.Kind = FnfComponentKind.Gratuity).Amount)
        Assert.Equal(12000D, p.Lines.Single(Function(l) l.Kind = FnfComponentKind.LeaveEncashment).Amount)
        Assert.Equal(19000D, p.Lines.Single(Function(l) l.Kind = FnfComponentKind.NoticePayRecovery).Amount)
        Assert.Equal(5000D, p.Lines.Single(Function(l) l.Kind = FnfComponentKind.LoanClosure).Amount)
        Assert.Equal(25000D, p.Lines.Single(Function(l) l.Kind = FnfComponentKind.LastSalary).Amount)
        Assert.Equal(6997D, p.Lines.Single(Function(l) l.Kind = FnfComponentKind.Bonus).Amount)
    End Sub

    <Fact>
    Public Sub NeedsFullAndFinalPlanAndViewPermission()
        _plan.Disabled.Add(PlanFeature.FullAndFinal)
        Assert.False(Plan().Succeeded)
        _plan.Disabled.Clear()
        _perms.Allowed.Clear()
        Assert.False(Plan().Succeeded)
    End Sub

    <Fact>
    Public Sub UnknownSeparation_Fails()
        _source.Facts = Nothing
        Assert.False(Plan().Succeeded)
    End Sub

    <Fact>
    Public Sub NoFactsSource_Fails()
        Dim bare As New FnfService(New FakeFnfRepository(), _perms, _plan, New FakePayAudit(), New FakePaySession())
        Assert.False(bare.Prepare(5, Nothing).Succeeded)
    End Sub

    <Fact>
    Public Sub OwnerChoices_BonusPctArrearsAndLastSalary()
        Dim p As FnfPlan = Plan(New FnfPrepareOptions With {.BonusPct = 20D, .ArrearsRecovery = 400D, .LastSalaryOverride = 30000D}).Value
        Assert.Equal(16800D, p.Lines.Single(Function(l) l.Kind = FnfComponentKind.Bonus).Amount)
        Assert.Equal(400D, p.Lines.Single(Function(l) l.Kind = FnfComponentKind.ArrearsRecovery).Amount)
        Assert.Equal(30000D, p.Lines.Single(Function(l) l.Kind = FnfComponentKind.LastSalary).Amount)
    End Sub

    <Fact>
    Public Sub NoRunCoversExit_LastSalaryZeroWithANote_AndTaxIsSkipped()
        _source.Facts.FinalRunId = Nothing
        Dim p As FnfPlan = Plan().Value
        Assert.False(p.Lines.Any(Function(l) l.Kind = FnfComponentKind.LastSalary))
        Assert.True(p.Notes.Any(Function(n) n.StartsWith("No run covers", StringComparison.Ordinal)))
        Assert.True(p.Notes.Any(Function(n) n.StartsWith("Income tax true-up skipped", StringComparison.Ordinal)))
        Assert.Equal(0, _tax.Calls)
    End Sub

    <Fact>
    Public Sub TaxDeductedInTheFinalRun_ComesBackAsARefund_WhenNoTaxIsDue()
        _source.Facts.FinalTdsAmount = 1500D
        Dim p As FnfPlan = Plan().Value
        Assert.Equal(1500D, p.Lines.Single(Function(l) l.Kind = FnfComponentKind.TdsRefund).Amount)
        Assert.Equal(1, _tax.Calls)
        Assert.Equal(9, _tax.LastRunId)
        Assert.Equal(2024, _tax.LastFinancialYear)
    End Sub

    <Fact>
    Public Sub PlanWithoutTds_NoTaxTrueUp()
        _plan.Disabled.Add(PlanFeature.Tds)
        _source.Facts.FinalTdsAmount = 1500D
        Dim p As FnfPlan = Plan().Value
        Assert.False(p.Lines.Any(Function(l) l.Kind = FnfComponentKind.TdsRefund))
        Assert.Equal(0, _tax.Calls)
    End Sub

    <Fact>
    Public Sub NotResigned_NoNoticeRecovery_ContractEmployeeGetsNoGratuity()
        _source.Facts.ReasonId = 5
        _source.Facts.EmploymentTypeId = 2
        Dim p As FnfPlan = Plan().Value
        Assert.False(p.Lines.Any(Function(l) l.Kind = FnfComponentKind.NoticePayRecovery))
        Assert.False(p.Lines.Any(Function(l) l.Kind = FnfComponentKind.Gratuity))
    End Sub

    <Fact>
    Public Sub Death_WaivesTheMinimumService()
        _source.Facts.JoinDate = New Date(2023, 4, 1)
        _source.Facts.ReasonId = 3
        Assert.True(Plan().Value.Lines.Any(Function(l) l.Kind = FnfComponentKind.Gratuity))
    End Sub

    <Fact>
    Public Sub LeaveTypesAreCutOneByOne_ThenAdded()
        _leave.Balances.Clear()
        _leave.Balances.Add(New LeaveBalanceRow(1, "EL", "Earned leave", True, 10D, 40D, 0D, 0D, 0D, 0D, 0D, 40D))
        _leave.Balances.Add(New LeaveBalanceRow(2, "PL", "Privilege leave", True, Nothing, 5D, 0D, 0D, 0D, 0D, 0D, 5D))
        _leave.Balances.Add(New LeaveBalanceRow(3, "CL", "Casual leave", False, Nothing, 8D, 0D, 0D, 0D, 0D, 0D, 8D))
        Assert.Equal(15000D, Plan().Value.Lines.Single(Function(l) l.Kind = FnfComponentKind.LeaveEncashment).Amount)
    End Sub

    <Fact>
    Public Sub NoRuleRows_NoBonusNoGratuity()
        _rates.GratuityRuleToReturn = Nothing
        _rates.BonusRuleToReturn = Nothing
        Dim p As FnfPlan = Plan().Value
        Assert.False(p.Lines.Any(Function(l) l.Kind = FnfComponentKind.Gratuity OrElse l.Kind = FnfComponentKind.Bonus))
    End Sub

    <Fact>
    Public Sub MissingTaxFacts_Fails()
        _tax.SkipEmployee = 101
        Assert.False(Plan().Succeeded)
    End Sub
End Class
