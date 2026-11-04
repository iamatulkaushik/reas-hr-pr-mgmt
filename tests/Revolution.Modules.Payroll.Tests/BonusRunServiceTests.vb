Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class BonusRunServiceTests
    Private ReadOnly _source As New FakeBonusSource()
    Private ReadOnly _runs As New FakeRunRepository()
    Private ReadOnly _salary As New FakeSalaryRepository()
    Private ReadOnly _rates As New FakeRates()
    Private ReadOnly _perms As New FakePayPermissions()
    Private ReadOnly _plan As New FakePayEntitlements()
    Private ReadOnly _audit As New FakePayAudit()
    Private ReadOnly _service As BonusRunService

    Private Shared ReadOnly PayMonth As New Date(2026, 11, 10)
    Private ReadOnly _tax As New FakeRunTaxSource()

    Public Sub New()
        For Each action As PermissionAction In [Enum].GetValues(GetType(PermissionAction))
            _perms.Allowed.Add(action)
        Next
        _rates.BonusRuleToReturn = New BonusRule(New Date(2020, 4, 1), 21000D, 7000D, 8.33D, 20D, 30, True, Nothing, "test")
        _service = New BonusRunService(_source, _runs, _salary, _rates, _perms, _plan, _audit, New FakePaySession())
        _source.Year(101, "E101", 15000D)
        _source.Year(102, "E102", 12000D)
    End Sub

    <Fact>
    Public Sub Preview_DefaultsToTheMinimumPct_AndAsksForTheYearFromApril()
        Dim r = _service.Preview(2025, PayMonth, Nothing)
        Assert.True(r.Succeeded)
        Assert.Equal(8.33D, r.Value.Pct)
        Assert.Equal(New Date(2025, 4, 1), _source.LoadedYear)
        Assert.Equal(2, r.Value.Rows.Count)
        Assert.Empty(_runs.Runs)
    End Sub

    <Fact>
    Public Sub Preview_PctOutsideTheRange_IsRefusedWithTheRange()
        Dim r = _service.Preview(2025, PayMonth, 25D)
        Assert.False(r.Succeeded)
        Assert.Contains("between 8.33% and 20%", r.Message, StringComparison.Ordinal)
        Assert.False(_service.Preview(2025, PayMonth, 5D).Succeeded)
    End Sub

    <Fact>
    Public Sub Preview_NoVerifiedRule_StopsWithItsMessage()
        _rates.BonusRuleToReturn = Nothing
        Dim r = _service.Preview(2025, PayMonth, Nothing)
        Assert.False(r.Succeeded)
        Assert.Equal("Not used in these tests.", r.Message)
    End Sub

    <Fact>
    Public Sub Preview_NoRunsInTheYear_Fails()
        _source.Months.Clear()
        Dim r = _service.Preview(2025, PayMonth, Nothing)
        Assert.False(r.Succeeded)
        Assert.Contains("No locked or paid payroll run", r.Message, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub Preview_BadYear_IsRefused()
        Assert.False(_service.Preview(1999, PayMonth, Nothing).Succeeded)
    End Sub

    <Fact>
    Public Sub Preview_WithoutView_OrWithoutPayrollInThePlan_IsRefused()
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.Preview(2025, PayMonth, Nothing).Succeeded)
        _perms.Allowed.Add(PermissionAction.View)
        _plan.PayrollEnabled = False
        Assert.False(_service.Preview(2025, PayMonth, Nothing).Succeeded)
    End Sub

    <Fact>
    Public Sub Create_MakesAnOffCycleRun_RegistersIt_AndSavesTheLines()
        Dim r = _service.Create(2025, New Date(2026, 11, 10), 10D)
        Assert.True(r.Succeeded)
        Assert.Equal(2, r.Value.RunNo)
        Assert.Equal(2, r.Value.Employees)
        Assert.Equal(r.Value.RunId, _source.RegisteredRun)
        Assert.Equal(New Date(2025, 4, 1), _source.RegisteredYear)
        Assert.Equal(10D, _source.RegisteredPct)
        Assert.Equal(2, _runs.Lines(r.Value.RunId).Count)
        Assert.Equal(PayrollRunStatus.Processed, _runs.StatusOf(r.Value.RunId))
        Assert.Equal(New Date(2026, 11, 1), _runs.Created.PeriodFrom)
        Assert.Equal(r.Value.Total, _runs.Lines(r.Value.RunId).Sum(Function(l) l.NetPay))
    End Sub

    <Fact>
    Public Sub Create_AuditHoldsNoAmounts()
        Dim r = _service.Create(2025, New Date(2026, 11, 10), Nothing)
        Assert.True(r.Succeeded)
        Assert.Equal("Bonus.Run", _audit.Entries.Last().Action)
        Assert.DoesNotContain(r.Value.Total.ToString(), _audit.Entries.Last().Details)
    End Sub

    <Fact>
    Public Sub Create_PayMonthBeforeTheYearEnds_IsRefused_AndNothingIsMade()
        Dim r = _service.Create(2025, New Date(2026, 3, 10), Nothing)
        Assert.False(r.Succeeded)
        Assert.Contains("after the accounting year ends", r.Message, StringComparison.Ordinal)
        Assert.Empty(_runs.Runs)
    End Sub

    <Fact>
    Public Sub Create_NothingToPay_Fails()
        _source.Paid(101) = 100000D
        _source.Paid(102) = 100000D
        Dim r = _service.Create(2025, New Date(2026, 11, 10), Nothing)
        Assert.False(r.Succeeded)
        Assert.Contains("No employee has a bonus", r.Message, StringComparison.Ordinal)
        Assert.Empty(_runs.Runs)
    End Sub

    <Fact>
    Public Sub Create_WithoutCreatePermission_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Create)
        Assert.False(_service.Create(2025, New Date(2026, 11, 10), Nothing).Succeeded)
        Assert.Empty(_runs.Runs)
    End Sub

    <Fact>
    Public Sub Create_RunCannotBeMade_ShowsTheDatabaseMessage()
        _runs.FailCreate = "An attendance month in the period is not closed."
        Dim r = _service.Create(2025, New Date(2026, 11, 10), Nothing)
        Assert.False(r.Succeeded)
        Assert.Equal("An attendance month in the period is not closed.", r.Message)
    End Sub

    <Fact>
    Public Sub Create_RegisterFails_ShowsTheMessage_AndNoLinesAreSaved()
        _source.FailRegister = "The run is already a bonus run."
        Dim r = _service.Create(2025, New Date(2026, 11, 10), Nothing)
        Assert.False(r.Succeeded)
        Assert.Equal("The run is already a bonus run.", r.Message)
        Assert.Empty(_runs.Lines)
    End Sub

    Private Function TaxedService(tax As IRunTaxSource) As BonusRunService
        Return New BonusRunService(_source, _runs, _salary, _rates, _perms, _plan, _audit, New FakePaySession(), tax, New TdsProjectionService(New FakeTdsRates()))
    End Function

    <Fact>
    Public Sub Preview_EpfMember_PaysProvidentFundOnTheBonus()
        _source.Flags(101) = New BonusEmployeeFlags(101, True, False, False)
        Dim r = _service.Preview(2025, PayMonth, Nothing)
        Assert.True(r.Succeeded)
        Assert.True(r.Value.Rows.Single(Function(x) x.EmployeeId = 101).EpfEmployee > 0D)
        Assert.Equal(0D, r.Value.Rows.Single(Function(x) x.EmployeeId = 102).EpfEmployee)
        Assert.True(r.Value.Lines.Single(Function(l) l.EmployeeId = 101).NetPay < r.Value.Lines.Single(Function(l) l.EmployeeId = 101).GrossEarnings)
    End Sub

    <Fact>
    Public Sub Preview_EpfRateMissing_StopsWithItsMessage()
        _rates.EpfFailure = "No EPF rate for the month."
        Dim r = _service.Preview(2025, PayMonth, Nothing)
        Assert.False(r.Succeeded)
        Assert.Equal("No EPF rate for the month.", r.Message)
    End Sub

    <Fact>
    Public Sub Preview_PayMonthBeforeTheYearEnds_IsRefused()
        Dim r = _service.Preview(2025, New Date(2026, 3, 10), Nothing)
        Assert.False(r.Succeeded)
        Assert.Contains("after the accounting year ends", r.Message, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub Preview_WithATaxSource_TakesIncomeTaxOnTheBonus_ForThePayMonthsFinancialYear()
        Dim tax As New FakeHighEarnerTaxSource()
        Dim r = TaxedService(tax).Preview(2025, PayMonth, Nothing)
        Assert.True(r.Succeeded)
        Assert.Equal(0, tax.LastRunId)
        Assert.Equal(2026, tax.LastFinancialYear)
        Dim row As BonusRow = r.Value.Rows.Single(Function(x) x.EmployeeId = 101)
        Assert.True(row.Tds > 0D)
        Assert.True(row.Tds < row.Amount)
        Assert.Equal(row.Tds, r.Value.Lines.Single(Function(l) l.EmployeeId = 101).TdsAmount)
        Assert.Equal(row.Amount - row.EpfEmployee - row.Tds, row.NetPay)
    End Sub

    <Fact>
    Public Sub Preview_PlanWithoutTds_TakesNoTax_AndNeverAsksForTaxFacts()
        _plan.Disabled.Add(PlanFeature.Tds)
        Dim tax As New FakeHighEarnerTaxSource()
        Dim r = TaxedService(tax).Preview(2025, PayMonth, Nothing)
        Assert.True(r.Succeeded)
        Assert.Equal(0, tax.Calls)
        Assert.All(r.Value.Lines, Sub(l) Assert.Equal(0D, l.TdsAmount))
    End Sub

    <Fact>
    Public Sub Create_TaxProblem_IsRefusedWithTheEmployeeCode_AndNothingIsMade()
        Dim tax As New FakeHighEarnerTaxSource() With {.SkipEmployee = 101}
        Dim r = TaxedService(tax).Create(2025, PayMonth, Nothing)
        Assert.False(r.Succeeded)
        Assert.Contains("E101: No tax facts were found.", r.Message, StringComparison.Ordinal)
        Assert.Empty(_runs.Runs)
    End Sub

    <Fact>
    Public Sub Create_SavesTheLinesWithProvidentFundAndTax_AndTheNetTotal()
        _source.Flags(101) = New BonusEmployeeFlags(101, True, False, False)
        Dim tax As New FakeHighEarnerTaxSource()
        Dim r = TaxedService(tax).Create(2025, PayMonth, Nothing)
        Assert.True(r.Succeeded)
        Dim saved As List(Of RunLineResult) = _runs.Lines(r.Value.RunId)
        Dim mine As RunLineResult = saved.Single(Function(l) l.EmployeeId = 101)
        Assert.True(mine.EpfEmployee > 0D)
        Assert.True(mine.TdsAmount > 0D)
        Assert.Equal(mine.GrossEarnings - mine.EpfEmployee - mine.TdsAmount, mine.NetPay)
        Assert.Equal(saved.Sum(Function(l) l.NetPay), r.Value.Total)
    End Sub
End Class
