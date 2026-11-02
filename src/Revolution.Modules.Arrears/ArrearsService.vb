Imports System.Globalization
Imports Revolution.Core
Imports Revolution.Modules.Payroll

''' <summary>
''' Arrears use cases: add a claim from the calculator's answer, list, open, cancel, and the report. Needs the BasicPayroll plan feature and Payroll permissions:
''' View to list, open and report, Create to add a claim, Edit to cancel one. A claim is never deleted. Audit text holds no amounts.
''' Contributions are worked at the rates of each wage month (Owner, 2026-11-01): the caller passes each month's rate rows in its ArrearsMonthInput.
''' </summary>
Public NotInheritable Class ArrearsService
    Public Const MaxMonths As Integer = 60
    Public Const MinReasonLength As Integer = 3
    Public Const MaxReasonLength As Integer = 200
    Private Const NotAllowed As String = "You are not allowed to do this."

    Private ReadOnly _repository As IArrearsRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext

    Public Sub New(repository As IArrearsRepository, permissions As IPermissionService, entitlements As IEntitlementService,
                   audit As IAuditService, session As ISessionContext)
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
    End Sub

    ''' <summary>Works out the arrears for the months given and stores the claim. Returns the claim id.</summary>
    Public Function Create(employeeId As Integer, reason As String, months As IReadOnlyList(Of ArrearsMonthInput)) As OperationResult(Of Integer)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Create)
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        Dim check As OperationResult = ValidateCreate(employeeId, reason, months)
        If Not check.Succeeded Then Return OperationResult(Of Integer).Fail(check.Message)
        Dim ordered As List(Of ArrearsMonthInput) = months.OrderBy(Function(m As ArrearsMonthInput) m.WageMonth).ToList()
        Dim answer As ArrearsResult = ArrearsCalculator.Calculate(ordered)
        Dim records As New List(Of ArrearsMonthRecord)()
        For i As Integer = 0 To ordered.Count - 1
            records.Add(ArrearsMonthRecord.From(ordered(i), answer.Months(i)))
        Next
        Dim id As Integer
        Try
            id = _repository.Add(New NewArrearsClaim(employeeId, reason.Trim(), records, CurrentUserId()))
        Catch ex As PayrollRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Payroll, "Arrears.Add", "ArrearsClaim", id.ToString(CultureInfo.InvariantCulture),
                                                                      $"Employee {employeeId.ToString(CultureInfo.InvariantCulture)}, {records.Count.ToString(CultureInfo.InvariantCulture)} months."))
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    Public Function List(filter As ArrearsFilter) As OperationResult(Of IReadOnlyList(Of ArrearsClaimRow))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of ArrearsClaimRow)).Fail(allowed.Message)
        Dim use As ArrearsFilter = If(filter, New ArrearsFilter())
        If use.FromMonth.HasValue AndAlso use.ToMonth.HasValue AndAlso use.ToMonth.Value < use.FromMonth.Value Then
            Return OperationResult(Of IReadOnlyList(Of ArrearsClaimRow)).Fail("The last month cannot be before the first month.")
        End If
        Return OperationResult(Of IReadOnlyList(Of ArrearsClaimRow)).Ok(_repository.List(use))
    End Function

    Public Function Open(claimId As Integer) As OperationResult(Of ArrearsClaimDetail)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of ArrearsClaimDetail).Fail(allowed.Message)
        If claimId <= 0 Then Return OperationResult(Of ArrearsClaimDetail).Fail("Pick a claim.")
        Try
            Return OperationResult(Of ArrearsClaimDetail).Ok(_repository.Get(claimId))
        Catch ex As PayrollRuleException
            Return OperationResult(Of ArrearsClaimDetail).Fail(ex.Message)
        End Try
    End Function

    ''' <summary>Cancels an open claim. The reason is kept. The months become free to claim again.</summary>
    Public Function Cancel(claimId As Integer, reason As String) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return allowed
        If claimId <= 0 Then Return OperationResult.Fail("Pick a claim.")
        Dim why As OperationResult = ValidateReason(reason)
        If Not why.Succeeded Then Return why
        Try
            _repository.Cancel(claimId, reason.Trim(), CurrentUserId())
        Catch ex As PayrollRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Payroll, "Arrears.Cancel", "ArrearsClaim", claimId.ToString(CultureInfo.InvariantCulture), "Claim cancelled."))
    End Function

    ''' <summary>The report for wage months fromMonth to toMonth (both snapped to the first of the month), open claims only.</summary>
    Public Function Report(fromMonth As Date, toMonth As Date) As OperationResult(Of ArrearsReport)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of ArrearsReport).Fail(allowed.Message)
        Dim first As New Date(fromMonth.Year, fromMonth.Month, 1)
        Dim last As New Date(toMonth.Year, toMonth.Month, 1)
        If last < first Then Return OperationResult(Of ArrearsReport).Fail("The last month cannot be before the first month.")
        If (last.Year - first.Year) * 12 + last.Month - first.Month + 1 > MaxMonths Then
            Return OperationResult(Of ArrearsReport).Fail($"The report covers at most {MaxMonths.ToString(CultureInfo.InvariantCulture)} months.")
        End If
        Return OperationResult(Of ArrearsReport).Ok(New ArrearsReport(first, last, _repository.Report(first, last)))
    End Function

    Private Shared Function ValidateCreate(employeeId As Integer, reason As String, months As IReadOnlyList(Of ArrearsMonthInput)) As OperationResult
        If employeeId <= 0 Then Return OperationResult.Fail("Pick an employee.")
        Dim why As OperationResult = ValidateReason(reason)
        If Not why.Succeeded Then Return why
        If months Is Nothing OrElse months.Count = 0 Then Return OperationResult.Fail("Give at least one wage month.")
        If months.Count > MaxMonths Then Return OperationResult.Fail($"A claim covers at most {MaxMonths.ToString(CultureInfo.InvariantCulture)} months.")
        If months.Select(Function(m As ArrearsMonthInput) m.WageMonth).Distinct().Count() <> months.Count Then Return OperationResult.Fail("A wage month is given twice.")
        Return OperationResult.Ok()
    End Function

    Private Shared Function ValidateReason(reason As String) As OperationResult
        Dim text As String = If(reason, String.Empty).Trim()
        If text.Length < MinReasonLength Then Return OperationResult.Fail("Give a reason.")
        If text.Length > MaxReasonLength Then Return OperationResult.Fail($"The reason can be at most {MaxReasonLength.ToString(CultureInfo.InvariantCulture)} characters.")
        Return OperationResult.Ok()
    End Function

    Private Function CheckAccess(action As PermissionAction) As OperationResult
        If Not _entitlements.IsEnabled(PlanFeature.BasicPayroll) Then Return OperationResult.Fail("Payroll is not part of your plan.")
        If Not _permissions.IsAllowed(ModuleKey.Payroll, action) Then Return OperationResult.Fail(NotAllowed)
        Return OperationResult.Ok()
    End Function

    Private Function CurrentUserId() As Integer?
        Dim user As UserSession = _session.Current
        Return If(user Is Nothing, CType(Nothing, Integer?), user.UserId)
    End Function
End Class
