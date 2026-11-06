Imports System.Globalization
Imports Revolution.Core
Imports Revolution.Modules.Attendance

''' <summary>
''' Marks attendance from punches (Phase 8 step 0503). For each employee with matched punches and each day in the range (at most nine days at a time): find the assigned shift, take the punches
''' in its window, set the in to the first and the out to the last, and when only one punch is found mark the exit automatically at the end of the assigned shift (Owner, 2026-11-04).
''' The attendance engine judges the day. A day is marked only when it has no mark yet; weekly off and holiday days, closed months, days with no shift, days that need a look and shifts
''' that have not ended are listed and left alone. Preview stores nothing and needs View on Biometric and Attendance. Apply needs Edit on Biometric and marks through AttendanceService, which
''' checks the month lock and the Attendance permission again. A punch is used for one day only. The audit text holds counts only.
''' </summary>
Public NotInheritable Class PunchAttendanceService
    Public Const MaxDays As Integer = 9
    Private Const NotAllowed As String = "You are not allowed to do this."

    Private ReadOnly _biometric As IBiometricRepository
    Private ReadOnly _roster As IRosterRepository
    Private ReadOnly _attendance As IAttendanceRepository
    Private ReadOnly _marker As AttendanceService
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _now As Func(Of Date)
    Private ReadOnly _policy As PunchWindowPolicy

    Public Sub New(biometric As IBiometricRepository, roster As IRosterRepository, attendance As IAttendanceRepository, marker As AttendanceService,
                   permissions As IPermissionService, entitlements As IEntitlementService, audit As IAuditService, now As Func(Of Date), Optional policy As PunchWindowPolicy = Nothing)
        If biometric Is Nothing Then Throw New ArgumentNullException(NameOf(biometric))
        If roster Is Nothing Then Throw New ArgumentNullException(NameOf(roster))
        If attendance Is Nothing Then Throw New ArgumentNullException(NameOf(attendance))
        If marker Is Nothing Then Throw New ArgumentNullException(NameOf(marker))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If now Is Nothing Then Throw New ArgumentNullException(NameOf(now))
        _biometric = biometric
        _roster = roster
        _attendance = attendance
        _marker = marker
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
        _now = now
        _policy = If(policy, New PunchWindowPolicy())
    End Sub

    Public Function Preview(fromDate As Date, toDate As Date) As OperationResult(Of PunchAttendancePlan)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of PunchAttendancePlan).Fail(allowed.Message)
        Return Build(fromDate, toDate)
    End Function

    Public Function Apply(fromDate As Date, toDate As Date) As OperationResult(Of PunchApplyResult)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return OperationResult(Of PunchApplyResult).Fail(allowed.Message)
        Dim planned As OperationResult(Of PunchAttendancePlan) = Build(fromDate, toDate)
        If Not planned.Succeeded Then Return OperationResult(Of PunchApplyResult).Fail(planned.Message)
        Dim marked As Integer
        Dim failures As New List(Of String)()
        For Each row As PunchAttendanceRow In planned.Value.Rows.Where(Function(r As PunchAttendanceRow) r.Action = PunchAttendanceAction.Mark)
            Dim r As OperationResult = _marker.MarkDay(row.EmployeeId, row.WorkDate, row.Status.Value, row.WorkedMinutes, row.OvertimeMinutes, row.ShiftId)
            If r.Succeeded Then
                marked += 1
            Else
                failures.Add($"Employee {row.EmployeeId.ToString(CultureInfo.InvariantCulture)}, {row.WorkDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)}: {r.Message}")
            End If
        Next
        Dim skipped As Integer = planned.Value.Rows.Count - marked - failures.Count
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Biometric, "Punch.Apply", "AttendanceDaily", "-",
                                                                      $"{marked.ToString(CultureInfo.InvariantCulture)} marked, {failures.Count.ToString(CultureInfo.InvariantCulture)} refused, {skipped.ToString(CultureInfo.InvariantCulture)} left alone."))
        If Not logged.Succeeded Then Return OperationResult(Of PunchApplyResult).Fail(logged.Message)
        Return OperationResult(Of PunchApplyResult).Ok(New PunchApplyResult(marked, skipped, failures))
    End Function

    Private Function Build(fromDate As Date, toDate As Date) As OperationResult(Of PunchAttendancePlan)
        Dim first As Date = fromDate.Date
        Dim last As Date = toDate.Date
        If last < first Then Return OperationResult(Of PunchAttendancePlan).Fail("The last date cannot be before the first date.")
        If (last - first).TotalDays + 1 > MaxDays Then Return OperationResult(Of PunchAttendancePlan).Fail($"Pick at most {MaxDays.ToString(CultureInfo.InvariantCulture)} days.")
        Dim shifts As Dictionary(Of Integer, ShiftItem) = _roster.ListShifts().Where(Function(s As ShiftItem) s.ShiftId.HasValue).ToDictionary(Function(s As ShiftItem) s.ShiftId.Value)
        Dim context As New Context(_roster.ListWeeklyOff().OrderBy(Function(w As WeeklyOffRule) w.EffectiveFrom).ToList(), Holidays(first, last), _attendance.ListLockedMonths())
        Dim punches As IEnumerable(Of IGrouping(Of Integer, EmployeePunchTime)) =
            _biometric.ListPunchTimes(first.AddDays(-1), last.AddDays(2)).GroupBy(Function(p As EmployeePunchTime) p.EmployeeId).OrderBy(Function(g As IGrouping(Of Integer, EmployeePunchTime)) g.Key)
        Dim rows As New List(Of PunchAttendanceRow)()
        For Each person As IGrouping(Of Integer, EmployeePunchTime) In punches
            rows.AddRange(ForEmployee(person.Key, person.Select(Function(p As EmployeePunchTime) p.PunchTime).OrderBy(Function(t As Date) t).ToList(), first, last, shifts, context))
        Next
        Return OperationResult(Of PunchAttendancePlan).Ok(New PunchAttendancePlan(first, last, rows))
    End Function

    Private Function ForEmployee(employeeId As Integer, times As List(Of Date), first As Date, last As Date, shifts As Dictionary(Of Integer, ShiftItem), context As Context) As List(Of PunchAttendanceRow)
        Dim rows As New List(Of PunchAttendanceRow)()
        Dim assignments As IReadOnlyList(Of EmployeeShiftItem) = _roster.ListEmployeeShifts(employeeId)
        Dim pool As List(Of Date) = times.ToList()
        Dim marked As New Dictionary(Of Integer, ISet(Of Date))()
        Dim day As Date = first
        While day <= last
            Dim assigned As EmployeeShiftItem = assignments.FirstOrDefault(Function(a As EmployeeShiftItem) a.EffectiveFrom <= day AndAlso (Not a.EffectiveTo.HasValue OrElse a.EffectiveTo.Value >= day))
            Dim item As ShiftItem = Nothing
            If assigned IsNot Nothing Then shifts.TryGetValue(assigned.ShiftId, item)
            If item Is Nothing Then
                If pool.Any(Function(t As Date) t.Date = day) Then rows.Add(Row(employeeId, day, Nothing, String.Empty, Nothing, PunchAttendanceAction.NoShift, "No shift is assigned for the day."))
            Else
                Dim row As PunchAttendanceRow = ForDay(employeeId, day, item, pool, marked, context)
                If row IsNot Nothing Then rows.Add(row)
            End If
            day = day.AddDays(1)
        End While
        Return rows
    End Function

    Private Function ForDay(employeeId As Integer, day As Date, item As ShiftItem, pool As List(Of Date), marked As Dictionary(Of Integer, ISet(Of Date)), context As Context) As PunchAttendanceRow
        Dim definition As ShiftDefinition = item.ToDefinition()
        Dim bounds As Tuple(Of Date, Date) = PunchDayBuilder.Window(definition, day, _policy)
        Dim inWindow As List(Of Date) = pool.Where(Function(t As Date) t >= bounds.Item1 AndAlso t <= bounds.Item2).ToList()
        Dim result As PunchDayResult = PunchDayBuilder.Build(definition, day, inWindow, _policy)
        If result.PunchCount = 0 Then Return Nothing
        pool.RemoveAll(Function(t As Date) t >= bounds.Item1 AndAlso t <= bounds.Item2)
        Dim shiftEnd As Date = day + item.StartTime + TimeSpan.FromMinutes(definition.SpanMinutes)
        If Not PunchDayBuilder.IsMarkable(result) Then Return Row(employeeId, day, item, result, PunchAttendanceAction.NeedsReview, If(result.Evaluation IsNot Nothing, result.Evaluation.Message, result.Note))
        If _now() < shiftEnd Then Return Row(employeeId, day, item, result, PunchAttendanceAction.ShiftNotEnded, "The shift has not ended yet.")
        If context.IsHoliday(day) Then Return Row(employeeId, day, item, result, PunchAttendanceAction.Holiday, "Punch on a holiday. Enter the day by hand.")
        If context.IsWeeklyOff(day) Then Return Row(employeeId, day, item, result, PunchAttendanceAction.WeeklyOff, "Punch on a weekly off day. Enter the day by hand.")
        If MonthLockPolicy.IsLocked(day, context.Locked) Then Return Row(employeeId, day, item, result, PunchAttendanceAction.MonthClosed, "The month is closed.")
        If IsMarked(employeeId, day, marked) Then Return Row(employeeId, day, item, result, PunchAttendanceAction.AlreadyMarked, "The day already has a mark.")
        Return Row(employeeId, day, item, result, PunchAttendanceAction.Mark, result.Note)
    End Function

    Private Function IsMarked(employeeId As Integer, day As Date, marked As Dictionary(Of Integer, ISet(Of Date))) As Boolean
        Dim key As Integer = day.Year * 100 + day.Month
        If Not marked.ContainsKey(key) Then
            marked(key) = New HashSet(Of Date)(_attendance.ListMonth(employeeId, day.Year, day.Month).Select(Function(d As DayRecord) d.Day.Date))
        End If
        Return marked(key).Contains(day.Date)
    End Function

    Private Shared Function Row(employeeId As Integer, day As Date, shiftId As Integer?, shiftName As String, result As PunchDayResult, action As PunchAttendanceAction, note As String) As PunchAttendanceRow
        Dim evaluation As DayEvaluation = If(result Is Nothing, Nothing, result.Evaluation)
        Dim judged As Boolean = evaluation IsNot Nothing AndAlso Not evaluation.NeedsReview
        Return New PunchAttendanceRow(employeeId, day, shiftId, shiftName, If(result Is Nothing, CType(Nothing, Date?), result.PunchIn), If(result Is Nothing, CType(Nothing, Date?), result.PunchOut),
                                      result IsNot Nothing AndAlso result.AutoExit, If(judged, CType(evaluation.Status, AttendanceStatus?), Nothing),
                                      If(judged, evaluation.WorkedMinutes, 0), If(judged, evaluation.OvertimeMinutes, 0), action, note)
    End Function

    Private Shared Function Row(employeeId As Integer, day As Date, item As ShiftItem, result As PunchDayResult, action As PunchAttendanceAction, note As String) As PunchAttendanceRow
        Return Row(employeeId, day, item.ShiftId, item.Name, result, action, note)
    End Function

    Private Function Holidays(first As Date, last As Date) As ISet(Of Date)
        Dim days As New HashSet(Of Date)()
        For year As Integer = first.Year To last.Year
            For Each h As HolidayItem In _roster.ListHolidays(year)
                days.Add(h.Day.Date)
            Next
        Next
        Return days
    End Function

    Private Function CheckAccess(action As PermissionAction) As OperationResult
        If Not _entitlements.IsEnabled(PlanFeature.Biometric) Then Return OperationResult.Fail("Biometric attendance is not part of your plan.")
        If Not _permissions.IsAllowed(ModuleKey.Biometric, action) OrElse Not _permissions.IsAllowed(ModuleKey.Attendance, If(action = PermissionAction.Edit, PermissionAction.Edit, PermissionAction.View)) Then
            Return OperationResult.Fail(NotAllowed)
        End If
        Return OperationResult.Ok()
    End Function

    Private NotInheritable Class Context
        Private ReadOnly _weeklyOff As List(Of WeeklyOffRule)
        Private ReadOnly _holidays As ISet(Of Date)

        Public Sub New(weeklyOff As List(Of WeeklyOffRule), holidays As ISet(Of Date), locked As ISet(Of Integer))
            _weeklyOff = weeklyOff
            _holidays = holidays
            Me.Locked = locked
        End Sub

        Public ReadOnly Property Locked As ISet(Of Integer)

        Public Function IsHoliday(day As Date) As Boolean
            Return _holidays.Contains(day.Date)
        End Function

        Public Function IsWeeklyOff(day As Date) As Boolean
            Dim rule As WeeklyOffRule = _weeklyOff.LastOrDefault(Function(w As WeeklyOffRule) w.EffectiveFrom <= day.Date)
            Return rule IsNot Nothing AndAlso rule.IsOff(day.DayOfWeek)
        End Function
    End Class
End Class
