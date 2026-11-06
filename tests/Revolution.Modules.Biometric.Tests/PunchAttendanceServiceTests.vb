Imports Revolution.Core
Imports Revolution.Modules.Attendance
Imports Revolution.Modules.Biometric
Imports Xunit

Public Class PunchAttendanceServiceTests
    Private Shared ReadOnly Monday As New Date(2026, 11, 2)

    Private ReadOnly _bio As New FakeBiometricRepository()
    Private ReadOnly _roster As New FakeRoster()
    Private ReadOnly _attendance As New FakeAttendanceRepository()
    Private ReadOnly _perms As New FakeBioPermissions()
    Private ReadOnly _plan As New FakeBioEntitlements()
    Private ReadOnly _audit As New FakeBioAudit()
    Private _now As Date = New Date(2026, 11, 10, 12, 0, 0)

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Edit)
        _roster.Shifts.Add(New ShiftItem(1, "Day", TimeSpan.FromHours(8), TimeSpan.FromHours(17), 60, 300, 480, 240, OvertimeRounding.QuarterHour, True))
        _roster.Shifts.Add(New ShiftItem(2, "Night", TimeSpan.FromHours(22), TimeSpan.FromHours(6), 30, 300, 420, 210, OvertimeRounding.QuarterHour, True))
        _roster.WeeklyOff.Add(New WeeklyOffRule(New Date(2026, 1, 1), 64))
        _roster.Holidays.Add(New HolidayItem(New Date(2026, 11, 5), "Festival"))
        Assign(101, 1)
        Assign(103, 2)
    End Sub

    Private Sub Assign(employeeId As Integer, shiftId As Integer)
        _roster.Assignments(employeeId) = New List(Of EmployeeShiftItem) From {New EmployeeShiftItem(shiftId, If(shiftId = 1, "Day", "Night"), New Date(2026, 1, 1), Nothing)}
    End Sub

    Private Sub Punch(employeeId As Integer, day As Date, hour As Integer, minute As Integer)
        _bio.PunchTimes.Add(New EmployeePunchTime(employeeId, day.Date.AddHours(hour).AddMinutes(minute)))
    End Sub

    Private Function Service() As PunchAttendanceService
        Dim marker As New AttendanceService(_attendance, _perms, _plan, _audit, New FakeBioSession(), Function() _now.Date)
        Return New PunchAttendanceService(_bio, _roster, _attendance, marker, _perms, _plan, _audit, Function() _now)
    End Function

    Private Shared Function RowOf(plan As PunchAttendancePlan, employeeId As Integer, day As Date) As PunchAttendanceRow
        Return plan.Rows.Single(Function(r) r.EmployeeId = employeeId AndAlso r.WorkDate = day.Date)
    End Function

    Private Sub BuildTheWeek()
        Punch(101, Monday, 7, 55)
        Punch(101, Monday, 17, 10)
        Punch(101, Monday.AddDays(1), 8, 2)
        Punch(101, Monday.AddDays(2), 8, 0)
        Punch(101, Monday.AddDays(2), 17, 0)
        Punch(101, Monday.AddDays(3), 8, 0)
        Punch(101, Monday.AddDays(3), 17, 0)
        Punch(101, Monday.AddDays(4), 18, 0)
        Punch(101, Monday.AddDays(6), 8, 0)
        Punch(101, Monday.AddDays(6), 17, 0)
        Punch(102, Monday, 8, 0)
        Punch(102, Monday, 17, 0)
        _attendance.Marked(101) = New List(Of DayRecord) From {New DayRecord(Monday.AddDays(2), AttendanceStatus.Present, 0)}
    End Sub

    <Fact>
    Public Sub Preview_FindsEachDay_AndSaysWhatWouldHappen()
        BuildTheWeek()
        Dim plan As PunchAttendancePlan = Service().Preview(Monday, Monday.AddDays(6)).Value
        Dim first As PunchAttendanceRow = RowOf(plan, 101, Monday)
        Assert.Equal(PunchAttendanceAction.Mark, first.Action)
        Assert.Equal(AttendanceStatus.Present, first.Status.Value)
        Assert.Equal(495, first.WorkedMinutes)
        Assert.Equal(15, first.OvertimeMinutes)
        Assert.False(first.AutoExit)
        Assert.Equal(1, first.ShiftId.Value)
        Dim lone As PunchAttendanceRow = RowOf(plan, 101, Monday.AddDays(1))
        Assert.Equal(PunchAttendanceAction.Mark, lone.Action)
        Assert.True(lone.AutoExit)
        Assert.Equal(Monday.AddDays(1).AddHours(17), lone.PunchOut.Value)
        Assert.Equal(PunchAttendanceAction.AlreadyMarked, RowOf(plan, 101, Monday.AddDays(2)).Action)
        Assert.Equal(PunchAttendanceAction.Holiday, RowOf(plan, 101, Monday.AddDays(3)).Action)
        Assert.Equal(PunchAttendanceAction.NeedsReview, RowOf(plan, 101, Monday.AddDays(4)).Action)
        Assert.Equal(PunchAttendanceAction.WeeklyOff, RowOf(plan, 101, Monday.AddDays(6)).Action)
        Assert.Equal(PunchAttendanceAction.NoShift, RowOf(plan, 102, Monday).Action)
        Assert.Equal(2, plan.Count(PunchAttendanceAction.Mark))
    End Sub

    <Fact>
    Public Sub Preview_StoresNothing()
        BuildTheWeek()
        Service().Preview(Monday, Monday.AddDays(6))
        Assert.Empty(_attendance.Saved)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub Apply_MarksOnlyTheDaysThatCanBeMarked_AndAudits()
        BuildTheWeek()
        Dim r As OperationResult(Of PunchApplyResult) = Service().Apply(Monday, Monday.AddDays(6))
        Assert.True(r.Succeeded)
        Assert.Equal(2, r.Value.Marked)
        Assert.Empty(r.Value.Failures)
        Assert.Equal(5, r.Value.Skipped)
        Assert.Equal(2, _attendance.Saved.Count)
        Dim saved As Tuple(Of Integer, DayRecord, Integer?, Integer) = _attendance.Saved.Single(Function(s) s.Item2.Day = Monday)
        Assert.Equal(101, saved.Item1)
        Assert.Equal(AttendanceStatus.Present, saved.Item2.Status)
        Assert.Equal(15, saved.Item2.OvertimeMinutes)
        Assert.Equal(1, saved.Item3.Value)
        Assert.Equal(495, saved.Item4)
        Assert.Equal("2 marked, 0 refused, 5 left alone.", _audit.Entries.Single(Function(e) e.Action = "Punch.Apply").Details)
    End Sub

    <Fact>
    Public Sub Apply_NeverOverwritesAMark_AndRunningItTwiceMarksNothingNew()
        BuildTheWeek()
        Service().Apply(Monday, Monday.AddDays(6))
        For Each s In _attendance.Saved.ToList()
            _attendance.Marked(s.Item1).Add(s.Item2)
        Next
        Dim again As OperationResult(Of PunchApplyResult) = Service().Apply(Monday, Monday.AddDays(6))
        Assert.Equal(0, again.Value.Marked)
        Assert.Equal(2, _attendance.Saved.Count)
    End Sub

    <Fact>
    Public Sub ClosedMonth_IsListedAndLeftAlone()
        _attendance.Locked.Add(202611)
        Punch(101, Monday, 8, 0)
        Punch(101, Monday, 17, 0)
        Dim row As PunchAttendanceRow = RowOf(Service().Preview(Monday, Monday).Value, 101, Monday)
        Assert.Equal(PunchAttendanceAction.MonthClosed, row.Action)
    End Sub

    <Fact>
    Public Sub ShiftNotEnded_IsNotMarked()
        _now = New Date(2026, 11, 3, 12, 0, 0)
        Punch(101, New Date(2026, 11, 3), 8, 0)
        Dim row As PunchAttendanceRow = RowOf(Service().Preview(New Date(2026, 11, 3), New Date(2026, 11, 3)).Value, 101, New Date(2026, 11, 3))
        Assert.Equal(PunchAttendanceAction.ShiftNotEnded, row.Action)
    End Sub

    <Fact>
    Public Sub NightShifts_OnTwoNights_GiveTwoDays_EachPunchUsedOnce()
        Punch(103, Monday, 21, 58)
        Punch(103, Monday.AddDays(1), 6, 5)
        Punch(103, Monday.AddDays(1), 21, 59)
        Punch(103, Monday.AddDays(2), 6, 3)
        Dim plan As PunchAttendancePlan = Service().Preview(Monday, Monday.AddDays(2)).Value
        Assert.Equal(2, plan.Rows.Count)
        Assert.Equal(Monday.AddHours(21).AddMinutes(58), RowOf(plan, 103, Monday).PunchIn.Value)
        Assert.Equal(Monday.AddDays(1).AddHours(6).AddMinutes(5), RowOf(plan, 103, Monday).PunchOut.Value)
        Assert.Equal(Monday.AddDays(1).AddHours(21).AddMinutes(59), RowOf(plan, 103, Monday.AddDays(1)).PunchIn.Value)
        Assert.All(plan.Rows, Sub(r) Assert.Equal(PunchAttendanceAction.Mark, r.Action))
    End Sub

    <Fact>
    Public Sub NoPunchesAtAll_GiveNoRows()
        Assert.Empty(Service().Preview(Monday, Monday.AddDays(2)).Value.Rows)
    End Sub

    <Fact>
    Public Sub Permissions_PreviewNeedsView_ApplyNeedsEdit_AndThePlan()
        BuildTheWeek()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.True(Service().Preview(Monday, Monday).Succeeded)
        Assert.False(Service().Apply(Monday, Monday).Succeeded)
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(Service().Preview(Monday, Monday).Succeeded)
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Edit)
        _plan.BiometricEnabled = False
        Assert.False(Service().Preview(Monday, Monday).Succeeded)
        Assert.Empty(_attendance.Saved)
    End Sub

    <Fact>
    Public Sub BadRanges_AreRefused()
        Assert.False(Service().Preview(Monday, Monday.AddDays(-1)).Succeeded)
        Assert.False(Service().Preview(Monday, Monday.AddDays(PunchAttendanceService.MaxDays)).Succeeded)
        Assert.True(Service().Preview(Monday, Monday.AddDays(PunchAttendanceService.MaxDays - 1)).Succeeded)
    End Sub
End Class
