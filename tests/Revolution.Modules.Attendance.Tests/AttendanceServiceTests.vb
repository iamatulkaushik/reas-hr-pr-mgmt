Imports Revolution.Core
Imports Revolution.Modules.Attendance
Imports Xunit

Public Class AttendanceServiceTests
    Private ReadOnly _repo As New FakeAttendanceRepository()
    Private ReadOnly _perms As New FakePermissions()
    Private ReadOnly _plan As New FakeEntitlements()
    Private ReadOnly _audit As New FakeAudit()
    Private ReadOnly _service As AttendanceService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Edit)
        _service = New AttendanceService(_repo, _perms, _plan, _audit, New FakeSession(), Function() New Date(2026, 11, 5))
    End Sub

    Private Shared Function FullMonth(year As Integer, month As Integer, markedDays As Integer) As MonthSummary
        Dim records As New List(Of DayRecord)
        For d As Integer = 1 To markedDays
            records.Add(New DayRecord(New Date(year, month, d), AttendanceStatus.Present, 0))
        Next
        Return MonthSummaryBuilder.Build(year, month, records).Value
    End Function

    Private Shared Function Summaries(summary As MonthSummary) As IReadOnlyDictionary(Of Integer, MonthSummary)
        Return New Dictionary(Of Integer, MonthSummary) From {{101, summary}}
    End Function

    <Fact>
    Public Sub MarkDay_Allowed_SavesAndAudits()
        Dim r = _service.MarkDay(101, New Date(2026, 11, 3), AttendanceStatus.Present, 480, 30, Nothing)
        Assert.True(r.Succeeded)
        Assert.Single(_repo.Saved)
        Assert.Equal("MarkDay", _audit.Entries(0).Action)
        Assert.Equal("101:03-11-2026", _audit.Entries(0).EntityId)
    End Sub

    <Fact>
    Public Sub MarkDay_WithoutEditPermission_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.MarkDay(101, New Date(2026, 11, 3), AttendanceStatus.Present, 480, 0, Nothing).Succeeded)
        Assert.Empty(_repo.Saved)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub MarkDay_InClosedMonth_IsRefused()
        _repo.Locked.Add(202610)
        Dim r = _service.MarkDay(101, New Date(2026, 10, 20), AttendanceStatus.Present, 480, 0, Nothing)
        Assert.False(r.Succeeded)
        Assert.Equal("The month is closed.", r.Message)
        Assert.Empty(_repo.Saved)
    End Sub

    <Fact>
    Public Sub MarkDay_ClosedBetweenCheckAndSave_ReturnsDatabaseMessage()
        _repo.FailWith = "The month is closed."
        Dim r = _service.MarkDay(101, New Date(2026, 11, 3), AttendanceStatus.Present, 480, 0, Nothing)
        Assert.False(r.Succeeded)
        Assert.Equal("The month is closed.", r.Message)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub MarkDay_FutureDay_IsRefused()
        Assert.False(_service.MarkDay(101, New Date(2026, 11, 6), AttendanceStatus.Present, 480, 0, Nothing).Succeeded)
    End Sub

    <Fact>
    Public Sub MarkDay_OvertimeOnPlanWithoutShiftOt_IsRefused()
        _plan.ShiftOtEnabled = False
        Assert.False(_service.MarkDay(101, New Date(2026, 11, 3), AttendanceStatus.Present, 540, 60, Nothing).Succeeded)
        Assert.False(_service.MarkDay(101, New Date(2026, 11, 3), AttendanceStatus.Present, 480, 0, 5).Succeeded)
        Assert.True(_service.MarkDay(101, New Date(2026, 11, 3), AttendanceStatus.Present, 480, 0, Nothing).Succeeded)
    End Sub

    <Fact>
    Public Sub MarkDay_OvertimeOnAbsentDay_IsRefused()
        Assert.False(_service.MarkDay(101, New Date(2026, 11, 3), AttendanceStatus.Absent, 0, 30, Nothing).Succeeded)
    End Sub

    <Fact>
    Public Sub MarkDay_BadMinutesOrEmployee_IsRefused()
        Assert.False(_service.MarkDay(101, New Date(2026, 11, 3), AttendanceStatus.Present, -1, 0, Nothing).Succeeded)
        Assert.False(_service.MarkDay(101, New Date(2026, 11, 3), AttendanceStatus.Present, 1441, 0, Nothing).Succeeded)
        Assert.False(_service.MarkDay(0, New Date(2026, 11, 3), AttendanceStatus.Present, 480, 0, Nothing).Succeeded)
    End Sub

    <Fact>
    Public Sub LoadMonth_BuildsSummary()
        _repo.Days.Add(New DayRecord(New Date(2026, 10, 1), AttendanceStatus.Present, 60))
        _repo.Days.Add(New DayRecord(New Date(2026, 10, 2), AttendanceStatus.HalfDay, 0))
        Dim r = _service.LoadMonth(101, 2026, 10)
        Assert.True(r.Succeeded)
        Assert.Equal(1.5D, r.Value.DaysPaid)
        Assert.Equal(29, r.Value.UnmarkedDays)
    End Sub

    <Fact>
    Public Sub LoadMonth_WithoutViewPermission_IsRefused()
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.LoadMonth(101, 2026, 10).Succeeded)
    End Sub

    <Fact>
    Public Sub CloseMonth_WithoutApprove_IsRefused()
        Assert.False(_service.CloseMonth(2026, 10, Summaries(FullMonth(2026, 10, 31))).Succeeded)
        Assert.Equal(0, _repo.ClosedKey)
    End Sub

    <Fact>
    Public Sub CloseMonth_CompleteEndedMonth_LocksAndAudits()
        _perms.Allowed.Add(PermissionAction.Approve)
        Dim r = _service.CloseMonth(2026, 10, Summaries(FullMonth(2026, 10, 31)))
        Assert.True(r.Succeeded)
        Assert.Equal(202610, _repo.ClosedKey)
        Assert.Equal("CloseMonth", _audit.Entries(0).Action)
    End Sub

    <Fact>
    Public Sub CloseMonth_WithUnmarkedDays_NamesTheEmployee()
        _perms.Allowed.Add(PermissionAction.Approve)
        Dim r = _service.CloseMonth(2026, 10, Summaries(FullMonth(2026, 10, 29)))
        Assert.False(r.Succeeded)
        Assert.StartsWith("Employee 101:", r.Message)
        Assert.Equal(0, _repo.ClosedKey)
    End Sub

    <Fact>
    Public Sub CloseMonth_BeforeMonthEnds_IsRefused()
        _perms.Allowed.Add(PermissionAction.Approve)
        Assert.False(_service.CloseMonth(2026, 11, Summaries(FullMonth(2026, 11, 30))).Succeeded)
    End Sub

    <Fact>
    Public Sub CloseMonth_SummaryOfOtherMonth_IsRefused()
        _perms.Allowed.Add(PermissionAction.Approve)
        Assert.False(_service.CloseMonth(2026, 10, Summaries(FullMonth(2026, 9, 30))).Succeeded)
    End Sub

    <Fact>
    Public Sub CloseMonth_NoEmployees_IsRefused()
        _perms.Allowed.Add(PermissionAction.Approve)
        Assert.False(_service.CloseMonth(2026, 10, New Dictionary(Of Integer, MonthSummary)()).Succeeded)
    End Sub

    <Fact>
    Public Sub ReopenMonth_WithApproveButNotReopen_IsRefused()
        _perms.Allowed.Add(PermissionAction.Approve)
        _repo.Locked.Add(202610)
        Assert.False(_service.ReopenMonth(2026, 10, "Wrong overtime entered for night shift").Succeeded)
        Assert.Contains(202610, _repo.Locked)
    End Sub

    <Fact>
    Public Sub ReopenMonth_ShortReason_IsRefused()
        _perms.Allowed.Add(PermissionAction.Reopen)
        _repo.Locked.Add(202610)
        Assert.False(_service.ReopenMonth(2026, 10, "fix").Succeeded)
        Assert.Contains(202610, _repo.Locked)
    End Sub

    <Fact>
    Public Sub ReopenMonth_WithReason_UnlocksAndAuditsReason()
        _perms.Allowed.Add(PermissionAction.Reopen)
        _repo.Locked.Add(202610)
        Dim r = _service.ReopenMonth(2026, 10, "  Wrong overtime entered for night shift  ")
        Assert.True(r.Succeeded)
        Assert.DoesNotContain(202610, _repo.Locked)
        Assert.Equal("ReopenMonth", _audit.Entries(0).Action)
        Assert.Equal("Reason: Wrong overtime entered for night shift", _audit.Entries(0).Details)
    End Sub

    <Fact>
    Public Sub ReopenMonth_NotClosed_ReturnsDatabaseMessage()
        _perms.Allowed.Add(PermissionAction.Reopen)
        _repo.FailWith = "The month is not closed."
        Dim r = _service.ReopenMonth(2026, 10, "Wrong overtime entered for night shift")
        Assert.False(r.Succeeded)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub ReopenMonth_OnPlanWithoutAuditTrail_StillWritesHistoryWithReasonAndUser()
        Dim silent As New AttendanceService(_repo, _perms, _plan, New SilentAudit(), New FakeSession(), Function() New Date(2026, 11, 5))
        _perms.Allowed.Add(PermissionAction.Reopen)
        _repo.Locked.Add(202610)
        Dim r = silent.ReopenMonth(2026, 10, "Wrong overtime entered for night shift")
        Assert.True(r.Succeeded)
        Assert.Equal("reopen:202610:7:Wrong overtime entered for night shift", _repo.History(0))
    End Sub

    <Fact>
    Public Sub ReopenMonth_RejectedReason_WritesNoHistory()
        _perms.Allowed.Add(PermissionAction.Reopen)
        _repo.Locked.Add(202610)
        Assert.False(_service.ReopenMonth(2026, 10, "short").Succeeded)
        Assert.Empty(_repo.History)
    End Sub

    <Fact>
    Public Sub ReopenMonth_DatabaseFailure_LeavesMonthLockedAndNoHistory()
        _perms.Allowed.Add(PermissionAction.Reopen)
        _repo.Locked.Add(202610)
        _repo.FailWith = "The month is not closed."
        Assert.False(_service.ReopenMonth(2026, 10, "Wrong overtime entered for night shift").Succeeded)
        Assert.Contains(202610, _repo.Locked)
        Assert.Empty(_repo.History)
    End Sub

    <Fact>
    Public Sub CloseMonth_WritesCloseHistory()
        _perms.Allowed.Add(PermissionAction.Approve)
        Assert.True(_service.CloseMonth(2026, 10, Summaries(FullMonth(2026, 10, 31))).Succeeded)
        Assert.Equal("close:202610", _repo.History(0))
    End Sub
End Class
