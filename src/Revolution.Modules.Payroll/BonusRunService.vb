Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' The annual bonus run (Bonuses screen). Preview shows what each employee is due for an accounting year (April to March) at a percentage inside the rule's range; Create makes an off-cycle run
''' (RunNo 2 and up) with the lines of the employees who have something to pay. The run is marked as the bonus run of the year, so a later run only pays the difference, and it then goes through the
''' normal approve, lock and pay steps; it cannot be processed like a regular run. The bonus rule must be verified (the rate service refuses it otherwise). Needs the BasicPayroll plan feature:
''' Payroll View to preview, Create to make the run. The bonus is paid after the accounting year ends, in the pay month given (the Owner decides the date; no deadline is checked). The attendance month of
''' the pay period must be closed, as for any run. The run carries provident fund on the bonus and, with the Tds plan feature and a tax source, the income tax on it (BonusTaxPlanner). The audit text holds no amounts.
''' </summary>
Public NotInheritable Class BonusRunService
    Private ReadOnly _source As IBonusSource
    Private ReadOnly _runs As IPayrollRunRepository
    Private ReadOnly _salary As ISalaryRepository
    Private ReadOnly _rates As IStatutoryRateService
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _tax As IRunTaxSource
    Private ReadOnly _projection As TdsProjectionService

    Public Sub New(source As IBonusSource, runs As IPayrollRunRepository, salary As ISalaryRepository, rates As IStatutoryRateService,
                   permissions As IPermissionService, entitlements As IEntitlementService, audit As IAuditService, session As ISessionContext,
                   Optional tax As IRunTaxSource = Nothing, Optional projection As TdsProjectionService = Nothing)
        If source Is Nothing Then Throw New ArgumentNullException(NameOf(source))
        If runs Is Nothing Then Throw New ArgumentNullException(NameOf(runs))
        If salary Is Nothing Then Throw New ArgumentNullException(NameOf(salary))
        If rates Is Nothing Then Throw New ArgumentNullException(NameOf(rates))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        _source = source
        _runs = runs
        _salary = salary
        _rates = rates
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
        _session = session
        _tax = tax
        _projection = projection
    End Sub

    ''' <summary>What the bonus run for the accounting year that starts in April of the given year would hold if paid in the pay month (any date in it). The percentage defaults to the rule's minimum. Nothing is stored.</summary>
    Public Function Preview(year As Integer, payMonth As Date, pct As Decimal?) As OperationResult(Of BonusRunPlan)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of BonusRunPlan).Fail(allowed.Message)
        Return Plan(year, payMonth, pct)
    End Function

    ''' <summary>Makes the bonus run for the accounting year, paid in the pay month (any date in it).</summary>
    Public Function Create(year As Integer, payMonth As Date, pct As Decimal?) As OperationResult(Of BonusRunResult)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Create)
        If Not allowed.Succeeded Then Return OperationResult(Of BonusRunResult).Fail(allowed.Message)
        Dim planned As OperationResult(Of BonusRunPlan) = Plan(year, payMonth, pct)
        If Not planned.Succeeded Then Return OperationResult(Of BonusRunResult).Fail(planned.Message)
        Dim bonusPlan As BonusRunPlan = planned.Value
        If bonusPlan.Problems.Count > 0 Then Return OperationResult(Of BonusRunResult).Fail(Summarise(bonusPlan.Problems))
        If bonusPlan.Lines.Count = 0 Then Return OperationResult(Of BonusRunResult).Fail("No employee has a bonus to pay for this year.")
        Dim policy As PolicySettings = _salary.GetPolicy()
        Dim period As RunPeriod = PayCycle.PeriodFor(payMonth.Year, payMonth.Month, policy.PayCycleStartDay)
        Dim user As Integer? = CurrentUserId()
        Dim runNo As Integer
        Dim runId As Integer
        Try
            runNo = _source.NextRunNo(period)
            runId = _runs.CreateRun(period, runNo, user)
            _source.Register(runId, New Date(year, 4, 1), bonusPlan.Pct, user)
            _runs.ReplaceLines(runId, bonusPlan.Lines, user)
        Catch ex As PayrollRuleException
            Return OperationResult(Of BonusRunResult).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Payroll, "Bonus.Run", "PayrollRun", runId.ToString(CultureInfo.InvariantCulture),
                                                                      $"Run {runNo.ToString(CultureInfo.InvariantCulture)}, accounting year {year.ToString(CultureInfo.InvariantCulture)}, {bonusPlan.Lines.Count.ToString(CultureInfo.InvariantCulture)} employees."))
        If Not logged.Succeeded Then Return OperationResult(Of BonusRunResult).Fail(logged.Message)
        Return OperationResult(Of BonusRunResult).Ok(New BonusRunResult(runId, runNo, bonusPlan.Lines.Count, bonusPlan.TotalAmount))
    End Function

    Private Function Plan(year As Integer, payMonth As Date, pct As Decimal?) As OperationResult(Of BonusRunPlan)
        If year < 2000 OrElse year > 9998 Then Return OperationResult(Of BonusRunPlan).Fail("The year is not valid.")
        If New Date(payMonth.Year, payMonth.Month, 1) < New Date(year + 1, 4, 1) Then
            Return OperationResult(Of BonusRunPlan).Fail("The bonus is paid after the accounting year ends. Choose a pay month from April " & (year + 1).ToString(CultureInfo.InvariantCulture) & ".")
        End If
        Dim yearFrom As New Date(year, 4, 1)
        Dim rule As OperationResult(Of BonusRule) = _rates.GetBonusRule(New Date(year + 1, 3, 31))
        If Not rule.Succeeded Then Return OperationResult(Of BonusRunPlan).Fail(rule.Message)
        Dim chosen As Decimal = If(pct, rule.Value.MinPct)
        If chosen < rule.Value.MinPct OrElse chosen > rule.Value.MaxPct Then
            Return OperationResult(Of BonusRunPlan).Fail($"The bonus must be between {rule.Value.MinPct.ToString("0.##", CultureInfo.InvariantCulture)}% and {rule.Value.MaxPct.ToString("0.##", CultureInfo.InvariantCulture)}%.")
        End If
        Dim facts As BonusFacts
        Try
            facts = _source.Load(yearFrom)
        Catch ex As PayrollRuleException
            Return OperationResult(Of BonusRunPlan).Fail(ex.Message)
        End Try
        If facts.Months.Count = 0 Then Return OperationResult(Of BonusRunPlan).Fail("No locked or paid payroll run was found in this accounting year.")
        Dim period As RunPeriod = PayCycle.PeriodFor(payMonth.Year, payMonth.Month, _salary.GetPolicy().PayCycleStartDay)
        Dim epf As OperationResult(Of EpfRate) = _rates.GetEpfRate(period.RateDate)
        If Not epf.Succeeded Then Return OperationResult(Of BonusRunPlan).Fail(epf.Message)
        Dim taxFor As Func(Of Integer, Decimal, OperationResult(Of Decimal)) = Nothing
        If _tax IsNot Nothing AndAlso _projection IsNot Nothing AndAlso _entitlements.IsEnabled(PlanFeature.Tds) Then
            Dim taxFacts As IReadOnlyDictionary(Of Integer, EmployeeTaxFacts)
            Try
                taxFacts = _tax.Load(0, FinancialYearCalendar.StartYear(period.PeriodTo), facts.Months.Select(Function(m As BonusMonthFact) m.EmployeeId).Distinct().OrderBy(Function(id As Integer) id).ToList())
            Catch ex As PayrollRuleException
                Return OperationResult(Of BonusRunPlan).Fail(ex.Message)
            End Try
            taxFor = Function(employeeId As Integer, amount As Decimal) As OperationResult(Of Decimal)
                         Dim mine As EmployeeTaxFacts = Nothing
                         If Not taxFacts.TryGetValue(employeeId, mine) Then Return OperationResult(Of Decimal).Fail("No tax facts were found.")
                         Return BonusTaxPlanner.Plan(period, amount, mine, AddressOf _projection.Project)
                     End Function
        End If
        Return OperationResult(Of BonusRunPlan).Ok(BonusRunBuilder.Build(rule.Value, facts, chosen, epf.Value, taxFor))
    End Function

    Private Shared Function Summarise(problems As IReadOnlyList(Of String)) As String
        Const shown As Integer = 5
        Dim text As String = String.Join(" ", problems.Take(shown))
        If problems.Count > shown Then text &= $" And {(problems.Count - shown).ToString(CultureInfo.InvariantCulture)} more."
        Return text
    End Function

    Private Function CheckAccess(action As PermissionAction) As OperationResult
        If Not _entitlements.IsEnabled(PlanFeature.BasicPayroll) Then Return OperationResult.Fail("Payroll is not part of your plan.")
        If Not _permissions.IsAllowed(ModuleKey.Payroll, action) Then Return OperationResult.Fail("You are not allowed to do this.")
        Return OperationResult.Ok()
    End Function

    Private Function CurrentUserId() As Integer?
        Dim user As UserSession = _session.Current
        Return If(user Is Nothing, CType(Nothing, Integer?), user.UserId)
    End Function
End Class
