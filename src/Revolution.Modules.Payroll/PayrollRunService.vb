Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' Payroll run use cases: create, process, submit, approve, return to draft, lock, apply loan recoveries, reopen, mark paid.
''' Needs the BasicPayroll feature and Payroll permissions: Create for a new run, Edit for process, submit, return, lock and paid, Approve for approval when the
''' plan needs a second person (otherwise Edit), Reopen for reopening a locked run, View for reads. The database checks the status table, the totals checksum and the
''' rule that the submitter cannot approve. Any missing rate, salary or attendance mark stops processing with the employee's code in the message. Audit text holds no amount.
''' </summary>
Public NotInheritable Class PayrollRunService
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const EntityName As String = "PayrollRun"
    Private Const ChecksumMismatchNumber As Integer = 50315
    Private Const RecoveriesAlreadyAppliedNumber As Integer = 50328
    Private Const MinReopenReasonLength As Integer = 10
    Private Const MaxReasonLength As Integer = 500
    Private Const MaxFailuresShown As Integer = 5
    Private Const DateFormat As String = "dd-MM-yyyy"

    Private ReadOnly _runs As IPayrollRunRepository
    Private ReadOnly _salary As ISalaryRepository
    Private ReadOnly _rates As IStatutoryRateService
    Private ReadOnly _approvals As IApprovalService
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _tax As IRunTaxSource
    Private ReadOnly _projection As TdsProjectionService
    Private _tdsWarnings As IReadOnlyList(Of String) = New List(Of String)()

    Public Sub New(runs As IPayrollRunRepository, salary As ISalaryRepository, rates As IStatutoryRateService, approvals As IApprovalService,
                   permissions As IPermissionService, entitlements As IEntitlementService, audit As IAuditService, session As ISessionContext,
                   Optional tax As IRunTaxSource = Nothing, Optional projection As TdsProjectionService = Nothing)
        If runs Is Nothing Then Throw New ArgumentNullException(NameOf(runs))
        If salary Is Nothing Then Throw New ArgumentNullException(NameOf(salary))
        If rates Is Nothing Then Throw New ArgumentNullException(NameOf(rates))
        If approvals Is Nothing Then Throw New ArgumentNullException(NameOf(approvals))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        _runs = runs
        _salary = salary
        _rates = rates
        _approvals = approvals
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
        _session = session
        _tax = tax
        _projection = projection
    End Sub

    ''' <summary>Employees whose tax pay could not cover in the last Process call, by code. Not stored: read it right after Process.</summary>
    Public ReadOnly Property TdsWarnings As IReadOnlyList(Of String)
        Get
            Return _tdsWarnings
        End Get
    End Property

    Public Function GetRun(runId As Integer) As OperationResult(Of PayrollRunHeader)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of PayrollRunHeader).Fail(allowed.Message)
        Dim header As PayrollRunHeader = _runs.GetRun(runId)
        If header Is Nothing Then Return OperationResult(Of PayrollRunHeader).Fail("Run not found.")
        Return OperationResult(Of PayrollRunHeader).Ok(header)
    End Function

    Public Function ListLines(runId As Integer) As OperationResult(Of IReadOnlyList(Of RunLineResult))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of RunLineResult)).Fail(allowed.Message)
        If _runs.GetRun(runId) Is Nothing Then Return OperationResult(Of IReadOnlyList(Of RunLineResult)).Fail("Run not found.")
        Return OperationResult(Of IReadOnlyList(Of RunLineResult)).Ok(_runs.ListLines(runId))
    End Function

    Public Function ListEvents(runId As Integer) As OperationResult(Of IReadOnlyList(Of PayrollRunEventItem))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of PayrollRunEventItem)).Fail(allowed.Message)
        If _runs.GetRun(runId) Is Nothing Then Return OperationResult(Of IReadOnlyList(Of PayrollRunEventItem)).Fail("Run not found.")
        Return OperationResult(Of IReadOnlyList(Of PayrollRunEventItem)).Ok(_runs.ListEvents(runId))
    End Function

    ''' <summary>Creates the regular run (run 1) for a wage month. The period follows the company pay cycle. Returns the run id.</summary>
    Public Function CreateRegularRun(year As Integer, month As Integer) As OperationResult(Of Integer)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Create)
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        If year < 2000 OrElse year > 9999 OrElse month < 1 OrElse month > 12 Then Return OperationResult(Of Integer).Fail("Month or year is not valid.")
        Dim policy As PolicySettings = _salary.GetPolicy()
        Dim period As RunPeriod = PayCycle.PeriodFor(year, month, policy.PayCycleStartDay)
        Dim id As Integer
        Try
            id = _runs.CreateRun(period, 1, CurrentUserId())
        Catch ex As PayrollRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = Log("Create", id, $"Period {period.PeriodFrom.ToString(DateFormat, CultureInfo.InvariantCulture)} to {period.PeriodTo.ToString(DateFormat, CultureInfo.InvariantCulture)}.")
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    ''' <summary>Calculates every employee and replaces the run's lines. Nothing is saved when any employee or rate fails. Returns the employee count.</summary>
    Public Function Process(runId As Integer) As OperationResult(Of Integer)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        Dim header As PayrollRunHeader = _runs.GetRun(runId)
        If header Is Nothing Then Return OperationResult(Of Integer).Fail("Run not found.")
        If Not RunStateMachine.CanEditLines(header.Status) Then Return OperationResult(Of Integer).Fail("The run cannot be processed in this status.")

        Dim policy As PolicySettings = _salary.GetPolicy()
        Dim period As RunPeriod = header.Period()
        Dim facts As IReadOnlyList(Of RunEmployeeFacts) = _runs.LoadInputs(period)
        If facts.Count = 0 Then Return OperationResult(Of Integer).Fail("No employee has pay in this period.")
        Dim rates As OperationResult(Of RunRates) = LoadRates(period, facts)
        If Not rates.Succeeded Then Return OperationResult(Of Integer).Fail(rates.Message)

        Dim taxFacts As IReadOnlyDictionary(Of Integer, EmployeeTaxFacts) = LoadTaxFacts(runId, period, facts)
        Dim lines As New List(Of RunLineResult)()
        Dim failures As New List(Of String)()
        Dim warnings As New List(Of String)()
        For Each f As RunEmployeeFacts In facts
            Dim r As OperationResult(Of RunLineResult) = PayrollRunCalculator.Calculate(f, period, policy, rates.Value, TaxHook(f, period, taxFacts))
            If r.Succeeded Then lines.Add(r.Value) Else failures.Add(r.Message)
            If r.Succeeded AndAlso r.Value.TdsShortfall > 0D Then warnings.Add(f.EmpCode)
        Next
        If failures.Count > 0 Then Return OperationResult(Of Integer).Fail(Summarise(failures))
        _tdsWarnings = warnings

        Try
            _runs.ReplaceLines(runId, lines, CurrentUserId())
        Catch ex As PayrollRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = Log("Process", runId, $"{lines.Count.ToString(CultureInfo.InvariantCulture)} employees.")
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(lines.Count)
    End Function

    ''' <summary>Tax facts for the whole run, or Nothing when the plan has no TDS feature or no tax source is set up.</summary>
    Private Function LoadTaxFacts(runId As Integer, period As RunPeriod, facts As IReadOnlyList(Of RunEmployeeFacts)) As IReadOnlyDictionary(Of Integer, EmployeeTaxFacts)
        If _tax Is Nothing OrElse _projection Is Nothing OrElse Not _entitlements.IsEnabled(PlanFeature.Tds) Then Return Nothing
        Return _tax.Load(runId, FinancialYearCalendar.StartYear(period.PeriodTo), facts.Select(Function(f) f.EmployeeId).ToList())
    End Function

    Private Function TaxHook(f As RunEmployeeFacts, period As RunPeriod, taxFacts As IReadOnlyDictionary(Of Integer, EmployeeTaxFacts)) As Func(Of WageBreakdown, OperationResult(Of Decimal))
        If taxFacts Is Nothing Then Return Nothing
        Return Function(wage)
                   Dim mine As EmployeeTaxFacts = Nothing
                   If Not taxFacts.TryGetValue(f.EmployeeId, mine) Then Return OperationResult(Of Decimal).Fail("No tax facts were found.")
                   Return RunTdsPlanner.Plan(period, wage, f.Salary, mine, AddressOf _projection.Project)
               End Function
    End Function

    ''' <summary>Sends a processed run for approval. Only for plans that need a second person; other plans approve directly.</summary>
    Public Function Submit(runId As Integer) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return allowed
        If Not _approvals.IsRequired(ModuleKey.Payroll) Then Return OperationResult.Fail("This plan does not need approval. Approve the run directly.")
        Dim header As PayrollRunHeader = _runs.GetRun(runId)
        If header Is Nothing Then Return OperationResult.Fail("Run not found.")
        If header.Status <> PayrollRunStatus.Processed Then Return OperationResult.Fail("Only a processed run can be submitted.")
        Dim number As Integer
        Dim changed As OperationResult = Change(runId, PayrollRunStatus.Submitted, "Submit", Nothing, number)
        If Not changed.Succeeded Then Return changed
        Dim summary As String = $"Payroll run {header.PeriodFrom.ToString(DateFormat, CultureInfo.InvariantCulture)} to {header.PeriodTo.ToString(DateFormat, CultureInfo.InvariantCulture)}, run {header.RunNo.ToString(CultureInfo.InvariantCulture)}, {header.EmployeeCount.ToString(CultureInfo.InvariantCulture)} employees."
        Dim request As OperationResult(Of Integer) = _approvals.Submit(ModuleKey.Payroll, EntityName, runId.ToString(CultureInfo.InvariantCulture), summary)
        If Not request.Succeeded Then
            Change(runId, PayrollRunStatus.Draft, "SubmitFailed", Nothing, number)
            Return OperationResult.Fail(request.Message)
        End If
        Return Log("Submit", runId, summary)
    End Function

    ''' <summary>
    ''' Approves the run. When the plan needs approval the run must be Submitted and the approval request approved; the database refuses the submitter.
    ''' When the totals changed after submission the approval is cancelled and the run goes back to Draft. Other plans approve a processed run directly.
    ''' </summary>
    Public Function Approve(runId As Integer) As OperationResult
        Dim required As Boolean = _approvals.IsRequired(ModuleKey.Payroll)
        Dim allowed As OperationResult = CheckAccess(If(required, PermissionAction.Approve, PermissionAction.Edit))
        If Not allowed.Succeeded Then Return allowed
        Dim header As PayrollRunHeader = _runs.GetRun(runId)
        If header Is Nothing Then Return OperationResult.Fail("Run not found.")
        If required Then
            If header.Status <> PayrollRunStatus.Submitted Then Return OperationResult.Fail("Only a submitted run can be approved.")
            If Not _approvals.IsCleared(ModuleKey.Payroll, EntityName, runId.ToString(CultureInfo.InvariantCulture)) Then
                Return OperationResult.Fail("The approval request has not been approved.")
            End If
        ElseIf header.Status <> PayrollRunStatus.Processed Then
            Return OperationResult.Fail("Only a processed run can be approved.")
        End If
        Dim number As Integer
        Dim changed As OperationResult = Change(runId, PayrollRunStatus.Approved, "Approve", Nothing, number)
        If Not changed.Succeeded Then
            If number = ChecksumMismatchNumber Then
                Dim ignored As Integer
                Change(runId, PayrollRunStatus.Draft, "ChecksumMismatch", "Totals changed after submission.", ignored)
                Return OperationResult.Fail(changed.Message & " The run is back in Draft.")
            End If
            Return changed
        End If
        Return Log("Approve", runId, String.Empty)
    End Function

    ''' <summary>Sends a processed or submitted run back to Draft so it can be processed again.</summary>
    Public Function ReturnToDraft(runId As Integer, reason As String) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return allowed
        If reason IsNot Nothing AndAlso reason.Trim().Length > MaxReasonLength Then Return OperationResult.Fail("Reason is too long (500 characters at most).")
        Dim header As PayrollRunHeader = _runs.GetRun(runId)
        If header Is Nothing Then Return OperationResult.Fail("Run not found.")
        If header.Status <> PayrollRunStatus.Processed AndAlso header.Status <> PayrollRunStatus.Submitted Then
            Return OperationResult.Fail("Only a processed or submitted run can go back to Draft.")
        End If
        Dim number As Integer
        Dim changed As OperationResult = Change(runId, PayrollRunStatus.Draft, "ReturnToDraft", CleanReason(reason), number)
        If Not changed.Succeeded Then Return changed
        Return Log("ReturnToDraft", runId, String.Empty)
    End Function

    ''' <summary>Locks an approved run and applies its loan recoveries. If the recoveries fail the run stays locked: use ApplyLoanRecoveries to retry.</summary>
    Public Function Lock(runId As Integer) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return allowed
        Dim header As PayrollRunHeader = _runs.GetRun(runId)
        If header Is Nothing Then Return OperationResult.Fail("Run not found.")
        If header.Status <> PayrollRunStatus.Approved Then Return OperationResult.Fail("Only an approved run can be locked.")
        Dim number As Integer
        Dim changed As OperationResult = Change(runId, PayrollRunStatus.Locked, "Lock", Nothing, number)
        If Not changed.Succeeded Then Return changed
        Dim applied As OperationResult = SaveRecoveries(header)
        If Not applied.Succeeded Then
            Return OperationResult.Fail("The run is locked but its loan recoveries were not saved: " & applied.Message & " Use Apply loan recoveries to try again.")
        End If
        Return Log("Lock", runId, String.Empty)
    End Function

    ''' <summary>Writes the loan recoveries of a locked run. Safe to repeat: recoveries already applied count as done.</summary>
    Public Function ApplyLoanRecoveries(runId As Integer) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return allowed
        Dim header As PayrollRunHeader = _runs.GetRun(runId)
        If header Is Nothing Then Return OperationResult.Fail("Run not found.")
        If header.Status <> PayrollRunStatus.Locked Then Return OperationResult.Fail("Loan recoveries are written only for a locked run.")
        Dim applied As OperationResult = SaveRecoveries(header)
        If Not applied.Succeeded Then Return applied
        Return Log("ApplyLoanRecoveries", runId, String.Empty)
    End Function

    ''' <summary>Reopens a locked run to Draft with a written reason of at least 10 characters, and reverses its loan recoveries.</summary>
    Public Function Reopen(runId As Integer, reason As String) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Reopen)
        If Not allowed.Succeeded Then Return allowed
        Dim cleaned As String = CleanReason(reason)
        If cleaned Is Nothing OrElse cleaned.Length < MinReopenReasonLength Then Return OperationResult.Fail("A written reason of at least 10 characters is required.")
        If cleaned.Length > MaxReasonLength Then Return OperationResult.Fail("Reason is too long (500 characters at most).")
        Dim header As PayrollRunHeader = _runs.GetRun(runId)
        If header Is Nothing Then Return OperationResult.Fail("Run not found.")
        If header.Status <> PayrollRunStatus.Locked Then Return OperationResult.Fail("Only a locked run can be reopened.")
        Dim number As Integer
        Dim changed As OperationResult = Change(runId, PayrollRunStatus.Draft, "Reopen", cleaned, number)
        If Not changed.Succeeded Then Return changed
        Try
            _runs.ReverseRecoveries(runId)
        Catch ex As PayrollRuleException
            Return OperationResult.Fail("The run is open again but its loan recoveries were not reversed: " & ex.Message)
        End Try
        Return Log("Reopen", runId, String.Empty)
    End Function

    ''' <summary>Marks a locked run as paid. A paid run cannot be reopened.</summary>
    Public Function MarkPaid(runId As Integer) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return allowed
        Dim header As PayrollRunHeader = _runs.GetRun(runId)
        If header Is Nothing Then Return OperationResult.Fail("Run not found.")
        If header.Status <> PayrollRunStatus.Locked Then Return OperationResult.Fail("Only a locked run can be marked paid.")
        Dim number As Integer
        Dim changed As OperationResult = Change(runId, PayrollRunStatus.Paid, "MarkPaid", Nothing, number)
        If Not changed.Succeeded Then Return changed
        Return Log("MarkPaid", runId, String.Empty)
    End Function

    Private Function LoadRates(period As RunPeriod, facts As IReadOnlyList(Of RunEmployeeFacts)) As OperationResult(Of RunRates)
        Dim epf As OperationResult(Of EpfRate) = _rates.GetEpfRate(period.RateDate)
        If Not epf.Succeeded Then Return OperationResult(Of RunRates).Fail(epf.Message)
        Dim esi As OperationResult(Of EsiRate) = _rates.GetEsiRate(period.RateDate)
        If Not esi.Succeeded Then Return OperationResult(Of RunRates).Fail(esi.Message)
        Dim wageRule As OperationResult(Of WageRule) = _rates.GetWageRule(period.RateDate)
        If Not wageRule.Succeeded Then Return OperationResult(Of RunRates).Fail(wageRule.Message)
        Dim pt As New Dictionary(Of Integer, PtRule)()
        For Each stateId As Integer In facts.Where(Function(f) f.PtStateId.HasValue).Select(Function(f) f.PtStateId.Value).Distinct().OrderBy(Function(s) s)
            Dim rule As OperationResult(Of PtRule) = _rates.GetPtRule(stateId, period.RateDate)
            If Not rule.Succeeded Then Return OperationResult(Of RunRates).Fail(rule.Message)
            pt(stateId) = rule.Value
        Next
        Return OperationResult(Of RunRates).Ok(New RunRates(epf.Value, esi.Value, wageRule.Value, pt))
    End Function

    Private Function SaveRecoveries(header As PayrollRunHeader) As OperationResult
        Dim monthKey As Integer = header.Period().MonthKey
        Dim lines As IReadOnlyList(Of RunLineResult) = _runs.ListLines(header.RunId)
        Dim open As ILookup(Of Integer, OpenInstallment) = _runs.ListOpenInstallments(monthKey).ToLookup(Function(i) i.EmployeeId, Function(i) i.Installment)
        Dim all As New List(Of RecoveryAllocation)()
        For Each line As RunLineResult In lines.Where(Function(l) l.LoanEmi > 0D)
            Try
                all.AddRange(LoanRecoveryAllocator.Allocate(line.LoanEmi, open(line.EmployeeId), monthKey))
            Catch ex As ArgumentOutOfRangeException
                Return OperationResult.Fail($"Employee {line.EmployeeId.ToString(CultureInfo.InvariantCulture)}: the EMI taken is more than is outstanding.")
            End Try
        Next
        If all.Count = 0 Then Return OperationResult.Ok()
        Try
            _runs.SaveRecoveries(header.RunId, all)
        Catch ex As PayrollRuleException When ex.ErrorNumber = RecoveriesAlreadyAppliedNumber
            Return OperationResult.Ok()
        Catch ex As PayrollRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return OperationResult.Ok()
    End Function

    Private Function Change(runId As Integer, toStatus As PayrollRunStatus, action As String, reason As String, ByRef errorNumber As Integer) As OperationResult
        errorNumber = 0
        Try
            _runs.ChangeStatus(runId, toStatus, action, reason)
        Catch ex As PayrollRuleException
            errorNumber = ex.ErrorNumber
            Return OperationResult.Fail(ex.Message)
        End Try
        Return OperationResult.Ok()
    End Function

    Private Function Log(action As String, runId As Integer, details As String) As OperationResult
        Return _audit.Record(New AuditEntry(ModuleKey.Payroll, "PayrollRun." & action, EntityName, runId.ToString(CultureInfo.InvariantCulture), details))
    End Function

    Private Shared Function CleanReason(reason As String) As String
        If String.IsNullOrWhiteSpace(reason) Then Return Nothing
        Return reason.Trim()
    End Function

    Private Shared Function Summarise(failures As IReadOnlyList(Of String)) As String
        Dim shown As String = String.Join(" ", failures.Take(MaxFailuresShown))
        If failures.Count > MaxFailuresShown Then
            shown &= $" And {(failures.Count - MaxFailuresShown).ToString(CultureInfo.InvariantCulture)} more."
        End If
        Return shown
    End Function

    Private Function CheckAccess(action As PermissionAction) As OperationResult
        If Not _entitlements.IsEnabled(PlanFeature.BasicPayroll) Then
            Return OperationResult.Fail("Payroll is not part of your plan.")
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
