Imports System.Globalization
Imports Revolution.Core
Imports Revolution.Modules.Payroll

''' <summary>
''' Pays the open arrears claims in an off-cycle run (RunNo 2 and up) for a pay month (Owner, 2026-11-03: head by head, no tax in this run). Needs the BasicPayroll plan feature and Payroll Create.
''' The run is made empty, the claims are marked Paid by it (all or none, so two people cannot pay one claim twice), and the lines are saved; if the lines cannot be saved the claims are
''' put back to Open. The run then goes through the normal approve, lock and pay steps, and cannot be processed like a regular run. The attendance month of the period must be closed,
''' as for any run. A claim that cannot be paid is left Open and named in Problems by employee code. The audit text holds no amounts.
''' </summary>
Public NotInheritable Class ArrearsRunService
    Private ReadOnly _repository As IArrearsRepository
    Private ReadOnly _source As IArrearsSource
    Private ReadOnly _runs As IPayrollRunRepository
    Private ReadOnly _salary As ISalaryRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext

    Public Sub New(repository As IArrearsRepository, source As IArrearsSource, runs As IPayrollRunRepository, salary As ISalaryRepository,
                   permissions As IPermissionService, entitlements As IEntitlementService, audit As IAuditService, session As ISessionContext)
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If source Is Nothing Then Throw New ArgumentNullException(NameOf(source))
        If runs Is Nothing Then Throw New ArgumentNullException(NameOf(runs))
        If salary Is Nothing Then Throw New ArgumentNullException(NameOf(salary))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        _repository = repository
        _source = source
        _runs = runs
        _salary = salary
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
        _session = session
    End Sub

    ''' <summary>What an arrears run for the pay month would hold. Nothing is stored.</summary>
    Public Function Preview(payMonth As Date) As OperationResult(Of ArrearsRunPlan)
        Dim allowed As OperationResult = CheckAccess()
        If Not allowed.Succeeded Then Return OperationResult(Of ArrearsRunPlan).Fail(allowed.Message)
        Return Plan(payMonth)
    End Function

    ''' <summary>Makes the arrears run for the pay month (any date in it) from every open claim that can be paid.</summary>
    Public Function Create(payMonth As Date) As OperationResult(Of ArrearsRunResult)
        Dim allowed As OperationResult = CheckAccess()
        If Not allowed.Succeeded Then Return OperationResult(Of ArrearsRunResult).Fail(allowed.Message)
        Dim planned As OperationResult(Of ArrearsRunPlan) = Plan(payMonth)
        If Not planned.Succeeded Then Return OperationResult(Of ArrearsRunResult).Fail(planned.Message)
        Dim runPlan As ArrearsRunPlan = planned.Value
        If runPlan.Lines.Count = 0 Then Return OperationResult(Of ArrearsRunResult).Fail(NothingToPay(runPlan))
        Dim policy As PolicySettings = _salary.GetPolicy()
        Dim period As RunPeriod = PayCycle.PeriodFor(payMonth.Year, payMonth.Month, policy.PayCycleStartDay)
        Dim user As Integer? = CurrentUserId()
        Dim runNo As Integer
        Dim runId As Integer
        Try
            runNo = _source.NextRunNo(period)
            runId = _runs.CreateRun(period, runNo, user)
            _repository.MarkPaid(runId, runPlan.ClaimIds)
        Catch ex As PayrollRuleException
            Return OperationResult(Of ArrearsRunResult).Fail(ex.Message)
        End Try
        Try
            _runs.ReplaceLines(runId, runPlan.Lines, user)
        Catch ex As PayrollRuleException
            ReleaseQuietly(runId)
            Return OperationResult(Of ArrearsRunResult).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Payroll, "Arrears.Run", "PayrollRun", runId.ToString(CultureInfo.InvariantCulture),
                                                                      $"Run {runNo.ToString(CultureInfo.InvariantCulture)}, {runPlan.Lines.Count.ToString(CultureInfo.InvariantCulture)} employees, {runPlan.ClaimIds.Count.ToString(CultureInfo.InvariantCulture)} claims."))
        If Not logged.Succeeded Then Return OperationResult(Of ArrearsRunResult).Fail(logged.Message)
        Return OperationResult(Of ArrearsRunResult).Ok(New ArrearsRunResult(runId, runNo, runPlan.Lines.Count, runPlan.ClaimIds.Count, runPlan.TotalGross, runPlan.TotalNet, runPlan.Problems))
    End Function

    Private Function Plan(payMonth As Date) As OperationResult(Of ArrearsRunPlan)
        Dim policy As PolicySettings = _salary.GetPolicy()
        Dim period As RunPeriod = PayCycle.PeriodFor(payMonth.Year, payMonth.Month, policy.PayCycleStartDay)
        Dim open As IReadOnlyList(Of ArrearsClaimRow) = _repository.List(New ArrearsFilter()).Where(Function(c As ArrearsClaimRow) c.Status = ArrearsClaimStatus.Open).ToList()
        If open.Count = 0 Then Return OperationResult(Of ArrearsRunPlan).Fail("There are no open arrears claims to pay.")
        Dim details As New List(Of ArrearsClaimDetail)()
        Try
            For Each row As ArrearsClaimRow In open
                details.Add(_repository.Get(row.ClaimId))
            Next
        Catch ex As PayrollRuleException
            Return OperationResult(Of ArrearsRunPlan).Fail(ex.Message)
        End Try
        Dim factories As New Dictionary(Of Integer, Integer)()
        For Each f As RunEmployeeFacts In _runs.LoadInputs(period)
            factories(f.EmployeeId) = f.FactoryId
        Next
        Return OperationResult(Of ArrearsRunPlan).Ok(ArrearsRunBuilder.Build(details, factories))
    End Function

    Private Sub ReleaseQuietly(runId As Integer)
        Try
            _repository.Release(runId)
        Catch ex As PayrollRuleException
            ' The run keeps its claims marked Paid: the person is told the run could not be filled, and an administrator releases it. Nothing more can be done here.
        End Try
    End Sub

    Private Shared Function NothingToPay(plan As ArrearsRunPlan) As String
        If plan.Problems.Count = 0 Then Return "There is nothing to pay in the open claims."
        Return "No claim can be paid. " & String.Join(" ", plan.Problems.Take(5))
    End Function

    Private Function CheckAccess() As OperationResult
        If Not _entitlements.IsEnabled(PlanFeature.BasicPayroll) Then Return OperationResult.Fail("Payroll is not part of your plan.")
        If Not _permissions.IsAllowed(ModuleKey.Payroll, PermissionAction.Create) Then Return OperationResult.Fail("You are not allowed to do this.")
        Return OperationResult.Ok()
    End Function

    Private Function CurrentUserId() As Integer?
        Dim user As UserSession = _session.Current
        Return If(user Is Nothing, CType(Nothing, Integer?), user.UserId)
    End Function
End Class
