Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' Monthly attendance use cases (ADR 0026): permission, plan, month lock, storage, audit. Returns OperationResult; never shows a message.
''' Same permissions as the daily service. The mode (Monthly or Daily) only chooses the screens; Daily needs PlanFeature.DailyAttendance.
''' </summary>
Public NotInheritable Class AttendanceMonthlyService
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const NotInPlan As String = "Attendance is not part of your plan."
    Private Const DailyNotInPlan As String = "Daily attendance is part of the Platinum plan."
    Private Const MaxDay As Integer = 31
    Private Const MaxOvertimeHours As Decimal = 744D
    Private Const MaxRemarksLength As Integer = 100
    Private Const MonthFormat As String = "MM-yyyy"

    Private ReadOnly _repository As IAttendanceMonthlyRepository
    Private ReadOnly _roster As IRosterRepository
    Private ReadOnly _attendance As AttendanceService
    Private ReadOnly _attendanceRepository As IAttendanceRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _today As Func(Of Date)

    Public Sub New(repository As IAttendanceMonthlyRepository, roster As IRosterRepository, attendance As AttendanceService,
                   attendanceRepository As IAttendanceRepository, permissions As IPermissionService, entitlements As IEntitlementService,
                   audit As IAuditService, session As ISessionContext, today As Func(Of Date))
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If roster Is Nothing Then Throw New ArgumentNullException(NameOf(roster))
        If attendance Is Nothing Then Throw New ArgumentNullException(NameOf(attendance))
        If attendanceRepository Is Nothing Then Throw New ArgumentNullException(NameOf(attendanceRepository))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        If today Is Nothing Then Throw New ArgumentNullException(NameOf(today))
        _repository = repository
        _roster = roster
        _attendance = attendance
        _attendanceRepository = attendanceRepository
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
        _session = session
        _today = today
    End Sub

    ''' <summary>The mode the menu opens: Daily only when the company chose it and its plan has DailyAttendance, otherwise Monthly.</summary>
    Public Function CurrentMode() As AttendanceMode
        If _repository.GetMode() = AttendanceMode.Daily AndAlso _entitlements.IsEnabled(PlanFeature.DailyAttendance) Then
            Return AttendanceMode.Daily
        End If
        Return AttendanceMode.Monthly
    End Function

    ''' <summary>Sets the company's mode. Needs the Edit permission; Daily needs the DailyAttendance plan feature.</summary>
    Public Function SetMode(mode As AttendanceMode) As OperationResult
        If Not _permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Edit) Then Return OperationResult.Fail(NotAllowed)
        If mode <> AttendanceMode.Monthly AndAlso mode <> AttendanceMode.Daily Then Return OperationResult.Fail("Attendance mode is not valid.")
        If mode = AttendanceMode.Daily AndAlso Not _entitlements.IsEnabled(PlanFeature.DailyAttendance) Then Return OperationResult.Fail(DailyNotInPlan)
        Try
            _repository.SetMode(mode)
        Catch ex As AttendanceRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Attendance, "SetAttendanceMode", "AttendanceSetting", "mode", mode.ToString()))
    End Function

    ''' <summary>True when the user may enter attendance and the plan has it. Used by the import before it reads a row.</summary>
    Public Function CanEdit() As Boolean
        Return _permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Edit) AndAlso _entitlements.IsEnabled(PlanFeature.Attendance)
    End Function

    ''' <summary>The handler of the grid and the import for one month (ADR 0025). A new handler per run, because it keeps the sheet's codes to report who is missing.</summary>
    Public Function CreateBulkHandler(periodStart As Date) As MonthlyAttendanceHandler
        Return New MonthlyAttendanceHandler(Me, periodStart)
    End Function

    ''' <summary>The columns of the grid and the import.</summary>
    Public Shared Function BulkTemplate() As ImportTemplate
        Return MonthlyAttendanceTemplate.Create()
    End Function

    ''' <summary>The month the screens start on: the month before the current one.</summary>
    Public Function DefaultPeriod() As Date
        Dim t = _today().Date
        Return New Date(t.Year, t.Month, 1).AddMonths(-1)
    End Function

    ''' <summary>Every active employee employed in the month with the saved counts (empty counts where nothing is saved).</summary>
    Public Function LoadMonth(periodStart As Date) As OperationResult(Of IReadOnlyList(Of MonthlyAttendanceRow))
        Dim gate = CheckAccess(PermissionAction.View, periodStart)
        If Not gate.Succeeded Then Return OperationResult(Of IReadOnlyList(Of MonthlyAttendanceRow)).Fail(gate.Message)
        Try
            Return OperationResult(Of IReadOnlyList(Of MonthlyAttendanceRow)).Ok(_repository.ListMonth(periodStart.Date))
        Catch ex As AttendanceRuleException
            Return OperationResult(Of IReadOnlyList(Of MonthlyAttendanceRow)).Fail(ex.Message)
        End Try
    End Function

    ''' <summary>Counts the company rules give for an employee's days in the month: weekly off days and holidays.</summary>
    Public Function RuleDefaults(row As MonthlyAttendanceRow) As OperationResult(Of RuleCounts)
        If row Is Nothing Then Throw New ArgumentNullException(NameOf(row))
        Dim gate = CheckAccess(PermissionAction.View, New Date(row.WindowFrom.Year, row.WindowFrom.Month, 1))
        If Not gate.Succeeded Then Return OperationResult(Of RuleCounts).Fail(gate.Message)
        Try
            Dim rules = _roster.ListWeeklyOff()
            Dim holidays = _roster.ListHolidays(row.WindowFrom.Year)
            Return OperationResult(Of RuleCounts).Ok(New RuleCounts(MonthlyRules.WeeklyOffDays(row.WindowFrom, row.WindowTo, rules),
                                                                    MonthlyRules.HolidayDays(row.WindowFrom, row.WindowTo, holidays, rules)))
        Catch ex As AttendanceRuleException
            Return OperationResult(Of RuleCounts).Fail(ex.Message)
        End Try
    End Function

    ''' <summary>
    ''' Saves one employee's month (an upsert). Needs Edit; refused for a closed month, a month not yet started, a count outside 0 to 31, overtime outside 0 to 744 hours,
    ''' or (with requireComplete) counts that do not add up to the days of employment. Without requireComplete an incomplete row is stored and listed as incomplete.
    ''' </summary>
    Public Function Save(periodStart As Date, row As MonthlyAttendanceRow, requireComplete As Boolean) As OperationResult
        If row Is Nothing Then Throw New ArgumentNullException(NameOf(row))
        Dim gate = CheckAccess(PermissionAction.Edit, periodStart)
        If Not gate.Succeeded Then Return gate
        Dim check = Validate(periodStart, row, requireComplete)
        If Not check.Succeeded Then Return check
        Try
            If IsClosed(periodStart) Then Return OperationResult.Fail("The month is closed.")
            _repository.Save(periodStart.Date, row, requireComplete, CurrentUserId())
        Catch ex As AttendanceRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Attendance, "SaveMonthly", "AttendanceMonthly",
                                            $"{row.EmployeeId.ToString(CultureInfo.InvariantCulture)}:{periodStart.ToString(MonthFormat, CultureInfo.InvariantCulture)}",
                                            $"Present {row.PresentDays}; half {row.HalfDays}; weekly off {row.WeeklyOffDays}; holiday {row.HolidayDays}; paid leave {row.PaidLeaveDays}; absent {row.AbsentDays}; overtime {row.OvertimeHours.ToString(CultureInfo.InvariantCulture)} h"))
    End Function

    ''' <summary>Deletes one employee's row for the month. Needs Delete; refused for a closed month.</summary>
    Public Function Delete(employeeId As Integer, periodStart As Date) As OperationResult
        Dim gate = CheckAccess(PermissionAction.Delete, periodStart)
        If Not gate.Succeeded Then Return gate
        Try
            If IsClosed(periodStart) Then Return OperationResult.Fail("The month is closed.")
            _repository.Delete(employeeId, periodStart.Date, CurrentUserId())
        Catch ex As AttendanceRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Attendance, "DeleteMonthly", "AttendanceMonthly",
                                            $"{employeeId.ToString(CultureInfo.InvariantCulture)}:{periodStart.ToString(MonthFormat, CultureInfo.InvariantCulture)}", "Row deleted"))
    End Function

    Public Function IsClosed(periodStart As Date) As Boolean
        Return MonthLockPolicy.IsLocked(periodStart, _attendanceRepository.ListLockedMonths())
    End Function

    ''' <summary>Month close sheet in monthly mode: one summary per employee, and a blocker for each employee with no row or an incomplete row.</summary>
    Public Function LoadCloseSheet(year As Integer, month As Integer) As OperationResult(Of CloseSheet)
        If month < 1 OrElse month > 12 OrElse year < 1 OrElse year > 9999 Then Return OperationResult(Of CloseSheet).Fail("Month or year is not valid.")
        Dim period As New Date(year, month, 1)
        Dim rows = LoadMonth(period)
        If Not rows.Succeeded Then Return OperationResult(Of CloseSheet).Fail(rows.Message)
        Dim summaries As New Dictionary(Of Integer, MonthSummary)()
        Dim blockers As New List(Of String)()
        For Each row In rows.Value
            Dim built = MonthSummaryBuilder.FromMonthly(year, month, row)
            If Not built.Succeeded Then
                blockers.Add($"{row.EmpCode} {row.EmployeeName}: {built.Message}")
                Continue For
            End If
            summaries(row.EmployeeId) = built.Value
            If Not row.HasRow Then
                blockers.Add($"{row.EmpCode} {row.EmployeeName}: no attendance entered.")
                Continue For
            End If
            Dim check = MonthLockPolicy.CanClose(built.Value, _today())
            If Not check.Succeeded Then blockers.Add($"{row.EmpCode} {row.EmployeeName}: {check.Message}")
        Next
        Return OperationResult(Of CloseSheet).Ok(New CloseSheet(year, month, summaries, blockers, IsClosed(period)))
    End Function

    ''' <summary>Closes the month only when no employee blocks it. The lock itself is the shared one (att.MonthLock).</summary>
    Public Function CloseMonth(year As Integer, month As Integer) As OperationResult
        Dim sheet = LoadCloseSheet(year, month)
        If Not sheet.Succeeded Then Return OperationResult.Fail(sheet.Message)
        If sheet.Value.IsClosed Then Return OperationResult.Fail("The month is already closed.")
        If sheet.Value.Blockers.Count > 0 Then Return OperationResult.Fail(sheet.Value.Blockers(0))
        Return _attendance.CloseMonth(year, month, sheet.Value.Summaries)
    End Function

    Private Function CheckAccess(action As PermissionAction, periodStart As Date) As OperationResult
        If Not _permissions.IsAllowed(ModuleKey.Attendance, action) Then Return OperationResult.Fail(NotAllowed)
        If Not _entitlements.IsEnabled(PlanFeature.Attendance) Then Return OperationResult.Fail(NotInPlan)
        If periodStart.Day <> 1 Then Return OperationResult.Fail("The month must start on the first day.")
        Return OperationResult.Ok()
    End Function

    Private Function Validate(periodStart As Date, row As MonthlyAttendanceRow, requireComplete As Boolean) As OperationResult
        Dim today = _today().Date
        If periodStart.Date > New Date(today.Year, today.Month, 1) Then Return OperationResult.Fail("The month has not started yet.")
        If row.EmployeeId <= 0 Then Return OperationResult.Fail("Employee is not valid.")
        Dim counts = {row.PresentDays, row.HalfDays, row.WeeklyOffDays, row.HolidayDays, row.PaidLeaveDays, row.AbsentDays}
        If counts.Any(Function(c) c < 0 OrElse c > MaxDay) Then Return OperationResult.Fail("A day count is between 0 and 31.")
        If row.OvertimeHours < 0D OrElse row.OvertimeHours > MaxOvertimeHours Then Return OperationResult.Fail("Overtime is between 0 and 744 hours.")
        If Decimal.Round(row.OvertimeHours, 2) <> row.OvertimeHours Then Return OperationResult.Fail("Overtime hours have at most two decimals.")
        If row.Remarks.Length > MaxRemarksLength Then Return OperationResult.Fail("Remarks are too long (100 characters at most).")
        If row.TotalDays > row.WindowDays Then Return OperationResult.Fail($"Days add up to {row.TotalDays} but the employee worked {row.WindowDays} days in the month.")
        If requireComplete AndAlso row.TotalDays <> row.WindowDays Then
            Return OperationResult.Fail($"Days add up to {row.TotalDays} of {row.WindowDays}. Fill Absent Days or the missing days.")
        End If
        Return OperationResult.Ok()
    End Function

    Private Function CurrentUserId() As Integer?
        Dim user = _session.Current
        Return If(user Is Nothing, CType(Nothing, Integer?), user.UserId)
    End Function
End Class

''' <summary>Weekly off days and holidays that the company rules give for one employee's days in a month.</summary>
Public NotInheritable Class RuleCounts
    Public Sub New(weeklyOffDays As Integer, holidayDays As Integer)
        Me.WeeklyOffDays = weeklyOffDays
        Me.HolidayDays = holidayDays
    End Sub

    Public ReadOnly Property WeeklyOffDays As Integer
    Public ReadOnly Property HolidayDays As Integer
End Class
