Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' Full and final use cases: save the calculated lines, approve, pay and reopen. Needs the FullAndFinal plan feature and Payroll permissions: View to read, Edit to
''' save lines, Approve to approve or pay, Reopen to send an approved settlement back. The person who calculated it cannot approve it. Audit text holds no amounts.
''' </summary>
Public NotInheritable Class FnfService
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const NoSettlement As String = "Calculate the settlement first."

    Private ReadOnly _repository As IFnfRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _source As IFnfFactsSource
    Private ReadOnly _rates As IStatutoryRateService
    Private ReadOnly _leave As ILeaveRepository
    Private ReadOnly _tax As IRunTaxSource
    Private ReadOnly _projection As TdsProjectionService

    Public Sub New(repository As IFnfRepository, permissions As IPermissionService, entitlements As IEntitlementService,
                   audit As IAuditService, session As ISessionContext, Optional source As IFnfFactsSource = Nothing, Optional rates As IStatutoryRateService = Nothing,
                   Optional leave As ILeaveRepository = Nothing, Optional tax As IRunTaxSource = Nothing, Optional projection As TdsProjectionService = Nothing)
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        _repository = repository
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
        _session = session
        _source = source
        _rates = rates
        _leave = leave
        _tax = tax
        _projection = projection
    End Sub

    ''' <summary>The settlement for a separation. Succeeds with Nothing when none is saved yet.</summary>
    Public Function GetSettlement(separationId As Integer) As OperationResult(Of FnfSettlementView)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of FnfSettlementView).Fail(allowed.Message)
        If separationId <= 0 Then Return OperationResult(Of FnfSettlementView).Fail("Pick a separation.")
        Return OperationResult(Of FnfSettlementView).Ok(_repository.Get(separationId))
    End Function

    ''' <summary>
    ''' Works out the settlement for a separation from the database facts, the bonus and gratuity rule rows, the leave ledger and the final tax true-up. Nothing is saved: the screen shows
    ''' the lines, the Owner may change them, then SaveLines stores them. Leave out of it anything the plan or the facts do not support; the plan's notes say what and why.
    ''' </summary>
    Public Function Prepare(separationId As Integer, options As FnfPrepareOptions) As OperationResult(Of FnfPlan)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of FnfPlan).Fail(allowed.Message)
        If separationId <= 0 Then Return OperationResult(Of FnfPlan).Fail("Pick a separation.")
        If _source Is Nothing OrElse _rates Is Nothing Then Return OperationResult(Of FnfPlan).Fail("Full and final facts are not set up.")
        Dim src As FnfSourceFacts = _source.Load(separationId)
        If src Is Nothing Then Return OperationResult(Of FnfPlan).Fail("Separation not found.")
        Dim choices As FnfPrepareOptions = If(options, New FnfPrepareOptions())
        Dim extra As New List(Of String)()
        Dim tds As OperationResult(Of Decimal) = TdsTrueUp(src, extra)
        If Not tds.Succeeded Then Return OperationResult(Of FnfPlan).Fail(tds.Message)
        Dim planned As OperationResult(Of FnfPlan) = FnfPlanner.Plan(BuildFacts(src, choices, tds.Value, extra))
        If Not planned.Succeeded Then Return planned
        Return OperationResult(Of FnfPlan).Ok(New FnfPlan(planned.Value.Lines, planned.Value.Totals, planned.Value.DueWindow, extra.Concat(planned.Value.Notes).ToList()))
    End Function

    Private Function BuildFacts(src As FnfSourceFacts, choices As FnfPrepareOptions, tdsAdjustment As Decimal, notes As List(Of String)) As FnfFacts
        Dim facts As New FnfFacts With {
            .EmployeeId = src.EmployeeId, .JoinDate = src.JoinDate, .LastWorkingDay = src.LastWorkingDay,
            .DailyWageGross = If(src.DailyWageDivisor > 0D, src.MonthlyGross / src.DailyWageDivisor, 0D),
            .NoticeApplies = src.ReasonId = 1, .NoticeDate = src.NoticeDate, .NoticePeriodDays = src.NoticePeriodDays,
            .GratuityCovered = src.EmploymentTypeId = 1 OrElse src.EmploymentTypeId = 3, .IsFixedTerm = src.EmploymentTypeId = 3,
            .WaiveGratuityMinimum = src.ReasonId = 3 OrElse src.ReasonId = 4, .GratuityWage = src.MonthlyBasicPlusDa,
            .LoanOutstanding = src.LoanOutstanding, .TdsAdjustment = tdsAdjustment, .ArrearsRecovery = choices.ArrearsRecovery}
        Dim gratuity As OperationResult(Of GratuityRule) = _rates.GetGratuityRule(src.LastWorkingDay)
        If gratuity.Succeeded Then facts.GratuityRule = gratuity.Value
        ApplyBonus(facts, src, choices)
        ApplyLastSalary(facts, src, choices, notes)
        ApplyLeave(facts, src.EmployeeId)
        Return facts
    End Function

    Private Sub ApplyBonus(facts As FnfFacts, src As FnfSourceFacts, choices As FnfPrepareOptions)
        Dim rule As OperationResult(Of BonusRule) = _rates.GetBonusRule(src.LastWorkingDay)
        If Not rule.Succeeded Then Return
        Dim wages As New List(Of Decimal)(src.BonusMonths.Select(Function(m) m.BasicPlusDa))
        Dim days As Decimal = src.BonusMonths.Sum(Function(m) m.PaidDays)
        If src.FinalRunId.HasValue Then
            wages.Add(src.FinalBasicPlusDa)
            days += src.FinalPaidDays
        End If
        facts.BonusRule = rule.Value
        facts.BonusLastMonthlyWage = src.MonthlyBasicPlusDa
        facts.BonusMonthlyWages = wages
        facts.BonusDaysWorked = CInt(Math.Floor(days))
        facts.BonusPct = If(choices.BonusPct, rule.Value.MinPct)
    End Sub

    Private Shared Sub ApplyLastSalary(facts As FnfFacts, src As FnfSourceFacts, choices As FnfPrepareOptions, notes As List(Of String))
        If choices.LastSalaryOverride.HasValue Then
            facts.LastSalaryNet = choices.LastSalaryOverride.Value
        ElseIf src.FinalRunId.HasValue Then
            facts.LastSalaryNet = src.FinalNetPay
        Else
            notes.Add("No run covers the exit date, so the last salary is zero. Enter it by hand.")
        End If
    End Sub

    ''' <summary>Encashable leave types only: each balance cut to its own limit, then added up. The planner gets the total with no further limit.</summary>
    Private Sub ApplyLeave(facts As FnfFacts, employeeId As Integer)
        If _leave Is Nothing Then Return
        Dim encashable As List(Of LeaveBalanceRow) = _leave.ListBalances(employeeId).Where(Function(b) b.IsEncashable AndAlso b.BalanceDays > 0D).ToList()
        facts.LeaveEncashable = _leave.ListBalances(employeeId).Any(Function(b) b.IsEncashable)
        facts.LeaveBalanceDays = encashable.Sum(Function(b) If(b.MaxEncashDays.HasValue, Math.Min(b.BalanceDays, b.MaxEncashDays.Value), b.BalanceDays))
        facts.LeaveMaxEncashDays = 0D
    End Sub

    ''' <summary>Final tax true-up: the year's tax on pay actually earned, less tax already deducted including the final run's. Skipped, with a note, when tax is off or no run covers the exit date.</summary>
    Private Function TdsTrueUp(src As FnfSourceFacts, notes As List(Of String)) As OperationResult(Of Decimal)
        If _tax Is Nothing OrElse _projection Is Nothing OrElse Not _entitlements.IsEnabled(PlanFeature.Tds) Then Return OperationResult(Of Decimal).Ok(0D)
        If Not src.FinalRunId.HasValue Then
            notes.Add("Income tax true-up skipped: no run covers the exit date.")
            Return OperationResult(Of Decimal).Ok(0D)
        End If
        Dim financialYear As Integer = If(src.FinalMonthKey Mod 100 >= 4, src.FinalMonthKey \ 100, src.FinalMonthKey \ 100 - 1)
        Dim loaded As IReadOnlyDictionary(Of Integer, EmployeeTaxFacts) = _tax.Load(src.FinalRunId.Value, financialYear, New List(Of Integer) From {src.EmployeeId})
        Dim mine As EmployeeTaxFacts = Nothing
        If Not loaded.TryGetValue(src.EmployeeId, mine) Then Return OperationResult(Of Decimal).Fail("No tax facts were found.")
        Dim earned As New Dictionary(Of Integer, TdsMonthFacts)()
        For Each month As TdsMonthFacts In mine.LockedMonths
            earned(month.MonthKey) = month
        Next
        Dim final As New TdsMonthFacts(src.FinalMonthKey, src.FinalBasicPlusDa, src.FinalHra, src.FinalGross)
        earned(src.FinalMonthKey) = If(earned.ContainsKey(src.FinalMonthKey), SumMonths(earned(src.FinalMonthKey), final), final)
        Dim input As New TdsProjectionInput(src.EmployeeId, financialYear, mine.DateOfBirth, src.FinalMonthKey, FnfTdsTrueUp.MonthsForExit(financialYear, src.FinalMonthKey, earned),
                                            mine.TaxDeductedBefore + src.FinalTdsAmount, mine.Declaration)
        Dim projected As OperationResult(Of TdsProjectionResult) = _projection.Project(input)
        If Not projected.Succeeded Then Return OperationResult(Of Decimal).Fail(projected.Message)
        Return OperationResult(Of Decimal).Ok(FnfTdsTrueUp.Adjustment(projected.Value))
    End Function

    Private Shared Function SumMonths(a As TdsMonthFacts, b As TdsMonthFacts) As TdsMonthFacts
        Return New TdsMonthFacts(a.MonthKey, a.BasicPlusDa + b.BasicPlusDa, a.HraReceived + b.HraReceived, a.TaxableSalary + b.TaxableSalary)
    End Function

    ''' <summary>Totals for lines the screen has not saved yet.</summary>
    Public Function Preview(lines As IReadOnlyList(Of FnfLine)) As OperationResult(Of FnfTotals)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of FnfTotals).Fail(allowed.Message)
        If lines Is Nothing OrElse lines.Count = 0 Then Return OperationResult(Of FnfTotals).Fail("Add at least one line.")
        Return OperationResult(Of FnfTotals).Ok(FnfCalculator.Total(lines))
    End Function

    ''' <summary>Stores the lines and moves the settlement to Calculated. Allowed until it is approved. Returns the settlement id.</summary>
    Public Function SaveLines(separationId As Integer, lines As IReadOnlyList(Of FnfLine)) As OperationResult(Of Integer)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        If separationId <= 0 Then Return OperationResult(Of Integer).Fail("Pick a separation.")
        If lines Is Nothing OrElse lines.Count = 0 Then Return OperationResult(Of Integer).Fail("Add at least one line.")
        Dim current As FnfSettlementView = _repository.Get(separationId)
        If current IsNot Nothing AndAlso Not FnfTransition.CanEdit(current.Status) Then Return OperationResult(Of Integer).Fail("An approved settlement cannot be changed. Reopen it first.")
        Dim id As Integer
        Try
            id = _repository.Save(separationId, lines)
        Catch ex As PayrollRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Payroll, "Fnf.Calculate", "FnfSettlement", id.ToString(CultureInfo.InvariantCulture),
                                                                      $"{lines.Count.ToString(CultureInfo.InvariantCulture)} lines."))
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    Public Function Approve(separationId As Integer) As OperationResult
        Return Move(separationId, FnfStatus.Approved, PermissionAction.Approve, "Fnf.Approve")
    End Function

    Public Function MarkPaid(separationId As Integer) As OperationResult
        Return Move(separationId, FnfStatus.Paid, PermissionAction.Approve, "Fnf.Pay")
    End Function

    ''' <summary>Sends an approved settlement back to Calculated so the lines can change.</summary>
    Public Function Reopen(separationId As Integer) As OperationResult
        Return Move(separationId, FnfStatus.Calculated, PermissionAction.Reopen, "Fnf.Reopen")
    End Function

    Private Function Move(separationId As Integer, target As FnfStatus, action As PermissionAction, auditAction As String) As OperationResult
        Dim allowed As OperationResult = CheckAccess(action)
        If Not allowed.Succeeded Then Return allowed
        If separationId <= 0 Then Return OperationResult.Fail("Pick a separation.")
        Dim current As FnfSettlementView = _repository.Get(separationId)
        If current Is Nothing Then Return OperationResult.Fail(NoSettlement)
        If Not FnfTransition.CanMove(current.Status, target) Then Return OperationResult.Fail($"A {current.Status} settlement cannot move to {target}.")
        Dim userId As Integer? = CurrentUserId()
        If target = FnfStatus.Approved AndAlso (Not userId.HasValue OrElse Not FnfTransition.CanApprove(current.MakerUserId, userId.Value)) Then
            Return OperationResult.Fail("The person who calculated the settlement cannot approve it.")
        End If
        Try
            _repository.ChangeStatus(current.FnfId, target)
        Catch ex As PayrollRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Payroll, auditAction, "FnfSettlement", current.FnfId.ToString(CultureInfo.InvariantCulture), String.Empty))
    End Function

    Private Function CheckAccess(action As PermissionAction) As OperationResult
        If Not _entitlements.IsEnabled(PlanFeature.FullAndFinal) Then
            Return OperationResult.Fail("Full and final is not part of your plan.")
        End If
        If Not _permissions.IsAllowed(ModuleKey.Payroll, action) Then
            Return OperationResult.Fail(NotAllowed)
        End If
        Return OperationResult.Ok()
    End Function

    Private Function CurrentUserId() As Integer?
        Dim user As UserSession = _session.Current
        Return If(user Is Nothing, CType(Nothing, Integer?), user.UserId)
    End Function
End Class
