Imports System.Globalization
Imports Revolution.Core

''' <summary>Attendance use cases: permission, plan, month lock, storage, audit. Returns OperationResult; never shows a message.</summary>
Public NotInheritable Class AttendanceService
    Private Const DateFormat As String = "dd-MM-yyyy"
    Private Const MaxMinutesPerDay As Integer = 1440
    Private Const MaxAuditDetailLength As Integer = 1900
    Private Const MaxReasonLength As Integer = 500
    Private Const NotAllowed As String = "You are not allowed to do this."

    Private ReadOnly _repository As IAttendanceRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _today As Func(Of Date)

    Public Sub New(repository As IAttendanceRepository, permissions As IPermissionService, entitlements As IEntitlementService,
                   audit As IAuditService, session As ISessionContext, today As Func(Of Date))
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        If today Is Nothing Then Throw New ArgumentNullException(NameOf(today))
        _repository = repository
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
        _session = session
        _today = today
    End Sub

    ''' <summary>Marks one day for one employee. Refused for a closed month, a future day, or overtime and shifts on a plan without ShiftOtLop.</summary>
    Public Function MarkDay(employeeId As Integer, day As Date, status As AttendanceStatus, workedMinutes As Integer,
                            overtimeMinutes As Integer, shiftId As Integer?) As OperationResult
        If Not _permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Edit) Then
            Return OperationResult.Fail(NotAllowed)
        End If
        Dim check = ValidateMark(employeeId, day, status, workedMinutes, overtimeMinutes, shiftId)
        If Not check.Succeeded Then
            Return check
        End If
        Try
            If MonthLockPolicy.IsLocked(day, _repository.ListLockedMonths()) Then
                Return OperationResult.Fail("The month is closed.")
            End If
            _repository.SaveDay(employeeId, New DayRecord(day, status, overtimeMinutes), shiftId, workedMinutes, CurrentUserId())
        Catch ex As AttendanceRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Attendance, "MarkDay", "AttendanceDaily",
                                            $"{employeeId.ToString(CultureInfo.InvariantCulture)}:{day.ToString(DateFormat, CultureInfo.InvariantCulture)}",
                                            $"Status {status}; worked {workedMinutes} min; overtime {overtimeMinutes} min"))
    End Function

    ''' <summary>Month summary for one employee, built from the marked days.</summary>
    Public Function LoadMonth(employeeId As Integer, year As Integer, month As Integer) As OperationResult(Of MonthSummary)
        If Not _permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.View) Then
            Return OperationResult(Of MonthSummary).Fail(NotAllowed)
        End If
        If employeeId <= 0 Then
            Return OperationResult(Of MonthSummary).Fail("Pick an employee.")
        End If
        If month < 1 OrElse month > 12 OrElse year < 1 OrElse year > 9999 Then
            Return OperationResult(Of MonthSummary).Fail("Month or year is not valid.")
        End If
        Return MonthSummaryBuilder.Build(year, month, _repository.ListMonth(employeeId, year, month))
    End Function

    ''' <summary>Closes a month for the company. The caller passes every active employee's summary; one blocked employee blocks the close.</summary>
    Public Function CloseMonth(year As Integer, month As Integer, summaries As IReadOnlyDictionary(Of Integer, MonthSummary)) As OperationResult
        If Not _permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Approve) Then
            Return OperationResult.Fail(NotAllowed)
        End If
        If summaries Is Nothing OrElse summaries.Count = 0 Then
            Return OperationResult.Fail("There are no employees to close.")
        End If
        Dim check = CheckAllCanClose(year, month, summaries)
        If Not check.Succeeded Then
            Return check
        End If
        Dim key = MonthLockPolicy.KeyOf(New Date(year, month, 1))
        Try
            _repository.CloseMonth(key, CurrentUserId())
        Catch ex As AttendanceRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Attendance, "CloseMonth", "MonthLock",
                                            key.ToString(CultureInfo.InvariantCulture), $"{summaries.Count} employees"))
    End Function

    ''' <summary>Reopens a closed month. Needs the Reopen permission and a written reason. The reason is stored with the month on every plan; the audit entry is extra and plan-gated.</summary>
    Public Function ReopenMonth(year As Integer, month As Integer, reason As String) As OperationResult
        If Not _permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Reopen) Then
            Return OperationResult.Fail(NotAllowed)
        End If
        Dim reasonCheck = MonthLockPolicy.ValidateReopenReason(reason)
        If Not reasonCheck.Succeeded Then
            Return reasonCheck
        End If
        If reason.Trim().Length > MaxReasonLength Then
            Return OperationResult.Fail($"Keep the reason under {MaxReasonLength} characters.")
        End If
        If month < 1 OrElse month > 12 OrElse year < 1 OrElse year > 9999 Then
            Return OperationResult.Fail("Month or year is not valid.")
        End If
        Dim key = MonthLockPolicy.KeyOf(New Date(year, month, 1))
        Try
            _repository.ReopenMonth(key, reason.Trim(), CurrentUserId())
        Catch ex As AttendanceRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Dim details = $"Reason: {reason.Trim()}"
        Return _audit.Record(New AuditEntry(ModuleKey.Attendance, "ReopenMonth", "MonthLock",
                                            key.ToString(CultureInfo.InvariantCulture), If(details.Length > MaxAuditDetailLength, details.Substring(0, MaxAuditDetailLength), details)))
    End Function

    Private Function ValidateMark(employeeId As Integer, day As Date, status As AttendanceStatus, workedMinutes As Integer,
                                  overtimeMinutes As Integer, shiftId As Integer?) As OperationResult
        If employeeId <= 0 Then
            Return OperationResult.Fail("Pick an employee.")
        End If
        If Not [Enum].IsDefined(GetType(AttendanceStatus), status) Then
            Return OperationResult.Fail("Unknown attendance status.")
        End If
        If day.Date > _today().Date Then
            Return OperationResult.Fail("You cannot mark a future day.")
        End If
        If workedMinutes < 0 OrElse workedMinutes > MaxMinutesPerDay OrElse overtimeMinutes < 0 OrElse overtimeMinutes > MaxMinutesPerDay Then
            Return OperationResult.Fail("Minutes must be between 0 and 1440.")
        End If
        If (overtimeMinutes > 0 OrElse shiftId.HasValue) AndAlso Not _entitlements.IsEnabled(PlanFeature.ShiftOtLop) Then
            Return OperationResult.Fail("Shifts and overtime are not part of your plan.")
        End If
        If overtimeMinutes > 0 AndAlso status <> AttendanceStatus.Present Then
            Return OperationResult.Fail("Overtime counts only on a Present day.")
        End If
        Return OperationResult.Ok()
    End Function

    Private Function CheckAllCanClose(year As Integer, month As Integer, summaries As IReadOnlyDictionary(Of Integer, MonthSummary)) As OperationResult
        If month < 1 OrElse month > 12 OrElse year < 1 OrElse year > 9999 Then
            Return OperationResult.Fail("Month or year is not valid.")
        End If
        For Each pair In summaries
            If pair.Value Is Nothing OrElse pair.Value.Year <> year OrElse pair.Value.Month <> month Then
                Return OperationResult.Fail($"Employee {pair.Key}: summary is for a different month.")
            End If
            Dim result = MonthLockPolicy.CanClose(pair.Value, _today())
            If Not result.Succeeded Then
                Return OperationResult.Fail($"Employee {pair.Key}: {result.Message}")
            End If
        Next
        Return OperationResult.Ok()
    End Function

    Private Function CurrentUserId() As Integer?
        Dim user = _session.Current
        Return If(user Is Nothing, CType(Nothing, Integer?), user.UserId)
    End Function
End Class
