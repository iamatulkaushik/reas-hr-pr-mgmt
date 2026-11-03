Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' Leave accrual (Owner, 2026-11-01): the company sets, per leave type, how many days are earned for every so many days worked; each month the accrual is previewed and posted once.
''' Days worked are days present (a half day counts half; weekly off, holiday and paid leave do not). Saving a rule and posting need Owner or Associate and Payroll Edit;
''' reading needs Payroll View. Needs the FullAndFinal plan feature, as the leave ledger does. The post works the entries again from fresh data and does not trust the preview.
''' A month later than the current one cannot be posted. Audit text holds no days.
''' </summary>
Public NotInheritable Class LeaveAccrualService
    Private Const NotAllowed As String = "You are not allowed to do this."

    Private ReadOnly _source As ILeaveAccrualSource
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _today As Func(Of Date)

    Public Sub New(source As ILeaveAccrualSource, permissions As IPermissionService, entitlements As IEntitlementService,
                   audit As IAuditService, session As ISessionContext, Optional today As Func(Of Date) = Nothing)
        If source Is Nothing Then Throw New ArgumentNullException(NameOf(source))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        _source = source
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
        _session = session
        _today = If(today, Function() Date.Today)
    End Sub

    Public Function ListSettings() As OperationResult(Of IReadOnlyList(Of LeaveAccrualSettingItem))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View, False)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of LeaveAccrualSettingItem)).Fail(allowed.Message)
        Return OperationResult(Of IReadOnlyList(Of LeaveAccrualSettingItem)).Ok(_source.ListSettings())
    End Function

    ''' <summary>Sets the rule for a leave type: earnedDays for every perWorkedDays days worked. maxBalance Nothing means no cap.</summary>
    Public Function SaveSetting(leaveTypeId As Integer, earnedDays As Decimal, perWorkedDays As Decimal, rounding As RateRounding, maxBalance As Decimal?) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit, True)
        If Not allowed.Succeeded Then Return allowed
        If leaveTypeId <= 0 Then Return OperationResult.Fail("Pick a leave type.")
        If Not [Enum].IsDefined(GetType(RateRounding), rounding) Then Return OperationResult.Fail("Pick how the fraction is rounded.")
        If earnedDays < 0D Then Return OperationResult.Fail("Earned days cannot be negative.")
        If perWorkedDays <= 0D Then Return OperationResult.Fail("Worked days must be more than zero.")
        If maxBalance.HasValue AndAlso maxBalance.Value < 0D Then Return OperationResult.Fail("The cap cannot be negative.")
        Dim setting As New LeaveAccrualSetting(earnedDays, perWorkedDays, rounding, maxBalance)
        Try
            _source.SaveSetting(leaveTypeId, setting, CurrentUserId())
        Catch ex As PayrollRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Payroll, "LeaveAccrual.SaveSetting", "LeaveType", leaveTypeId.ToString(CultureInfo.InvariantCulture), "Accrual rule saved."))
    End Function

    ''' <summary>What posting would give for a leave type and a month (any date in the month). Nothing is stored.</summary>
    Public Function Preview(leaveTypeId As Integer, month As Date) As OperationResult(Of LeaveAccrualPreview)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View, False)
        If Not allowed.Succeeded Then Return OperationResult(Of LeaveAccrualPreview).Fail(allowed.Message)
        Return Work(leaveTypeId, month)
    End Function

    ''' <summary>Posts the month's accrual to the ledger, once per leave type and month. Returns the number of employees who earned leave.</summary>
    Public Function Post(leaveTypeId As Integer, month As Date) As OperationResult(Of Integer)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit, True)
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        Dim first As New Date(month.Year, month.Month, 1)
        If first > New Date(_today().Year, _today().Month, 1) Then Return OperationResult(Of Integer).Fail("A month that has not started cannot be posted.")
        Dim worked As OperationResult(Of LeaveAccrualPreview) = Work(leaveTypeId, month)
        If Not worked.Succeeded Then Return OperationResult(Of Integer).Fail(worked.Message)
        Dim count As Integer
        Try
            count = _source.Post(leaveTypeId, first, worked.Value.Entries, CurrentUserId())
        Catch ex As PayrollRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Payroll, "LeaveAccrual.Post", "LeaveType", leaveTypeId.ToString(CultureInfo.InvariantCulture),
                                                                      $"Month {first.ToString("yyyy-MM", CultureInfo.InvariantCulture)}, {count.ToString(CultureInfo.InvariantCulture)} employees."))
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(count)
    End Function

    Private Function Work(leaveTypeId As Integer, month As Date) As OperationResult(Of LeaveAccrualPreview)
        If leaveTypeId <= 0 Then Return OperationResult(Of LeaveAccrualPreview).Fail("Pick a leave type.")
        Dim first As New Date(month.Year, month.Month, 1)
        Dim item As LeaveAccrualSettingItem = _source.ListSettings().FirstOrDefault(Function(s As LeaveAccrualSettingItem) s.LeaveTypeId = leaveTypeId)
        If item Is Nothing Then Return OperationResult(Of LeaveAccrualPreview).Fail("Leave type not found.")
        If item.Setting Is Nothing Then Return OperationResult(Of LeaveAccrualPreview).Fail("Set the accrual rule for this leave type first.")
        Dim inputs As IReadOnlyList(Of LeaveAccrualInput) = _source.ListPresent(leaveTypeId, first)
        Dim entries As New List(Of LeaveAccrualEntry)()
        For Each input As LeaveAccrualInput In inputs
            Dim days As Decimal = LeaveAccrualCalculator.Accrue(input.PresentDays, Math.Max(0D, input.Balance), item.Setting)
            If days > 0D Then entries.Add(New LeaveAccrualEntry(input.EmployeeId, input.PresentDays, input.Balance, days))
        Next
        Return OperationResult(Of LeaveAccrualPreview).Ok(New LeaveAccrualPreview(leaveTypeId, first, inputs.Count, entries))
    End Function

    Private Function CheckAccess(action As PermissionAction, ownerOrAssociate As Boolean) As OperationResult
        If Not _entitlements.IsEnabled(PlanFeature.FullAndFinal) Then Return OperationResult.Fail("Leave is not part of your plan.")
        If Not _permissions.IsAllowed(ModuleKey.Payroll, action) Then Return OperationResult.Fail(NotAllowed)
        If ownerOrAssociate Then
            Dim user As UserSession = _session.Current
            If user Is Nothing OrElse (user.Role <> UserRole.Owner AndAlso user.Role <> UserRole.Associate) Then Return OperationResult.Fail(NotAllowed)
        End If
        Return OperationResult.Ok()
    End Function

    Private Function CurrentUserId() As Integer?
        Dim user As UserSession = _session.Current
        Return If(user Is Nothing, CType(Nothing, Integer?), user.UserId)
    End Function
End Class
