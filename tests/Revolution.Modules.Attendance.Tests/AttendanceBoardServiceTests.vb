Imports Revolution.Core
Imports Revolution.Modules.Attendance
Imports Xunit

Public Class AttendanceBoardServiceTests
    Private ReadOnly _repo As New FakeAttendanceRepository()
    Private ReadOnly _board As New FakeBoardRepository()
    Private ReadOnly _perms As New FakePermissions()
    Private ReadOnly _service As AttendanceBoardService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Edit)
        _perms.Allowed.Add(PermissionAction.Approve)
        Dim today As Func(Of Date) = Function() New Date(2026, 11, 5)
        Dim attendance As New AttendanceService(_repo, _perms, New FakeEntitlements(), New FakeAudit(), New FakeSession(), today)
        _service = New AttendanceBoardService(_board, attendance, _repo, _perms, today)
    End Sub

    Private Sub AddMonth(employeeId As Integer, code As String, markedDays As Integer)
        If markedDays = 0 Then
            _board.Month.Add(New MonthDayRow(employeeId, code, "Name " & code, Nothing))
            Return
        End If
        For d As Integer = 1 To markedDays
            _board.Month.Add(New MonthDayRow(employeeId, code, "Name " & code, New DayRecord(New Date(2026, 10, d), AttendanceStatus.Present, 0)))
        Next
    End Sub

    <Fact>
    Public Sub LoadDay_WithoutView_IsRefused()
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.LoadDay(New Date(2026, 11, 3)).Succeeded)
    End Sub

    <Fact>
    Public Sub SaveDay_SavesOnlyMarkedRows()
        Dim rows = New List(Of AttendanceSheetRow) From {
            New AttendanceSheetRow(1, "E1", "A", AttendanceStatus.Present, Nothing, 0, 0),
            New AttendanceSheetRow(2, "E2", "B", Nothing, Nothing, 0, 0)}
        Assert.True(_service.SaveDay(New Date(2026, 11, 3), rows).Succeeded)
        Assert.Single(_repo.Saved)
    End Sub

    <Fact>
    Public Sub SaveDay_ClosedMonth_ReportsEachRow()
        _repo.Locked.Add(202611)
        Dim rows = New List(Of AttendanceSheetRow) From {New AttendanceSheetRow(1, "E1", "A", AttendanceStatus.Present, Nothing, 0, 0)}
        Dim r = _service.SaveDay(New Date(2026, 11, 3), rows)
        Assert.False(r.Succeeded)
        Assert.Contains("E1: The month is closed.", r.Message)
    End Sub

    <Fact>
    Public Sub CloseSheet_EveryDayMarked_CanClose()
        AddMonth(1, "E1", 31)
        AddMonth(2, "E2", 31)
        Dim sheet = _service.LoadCloseSheet(2026, 10).Value
        Assert.True(sheet.CanClose)
        Assert.Equal(2, sheet.Summaries.Count)
    End Sub

    <Fact>
    Public Sub CloseSheet_UnmarkedEmployee_Blocks()
        AddMonth(1, "E1", 31)
        AddMonth(2, "E2", 20)
        AddMonth(3, "E3", 0)
        Dim sheet = _service.LoadCloseSheet(2026, 10).Value
        Assert.False(sheet.CanClose)
        Assert.Equal(2, sheet.Blockers.Count)
        Assert.Contains("E2", sheet.Blockers(0))
    End Sub

    <Fact>
    Public Sub CloseMonth_Blocked_DoesNotClose()
        AddMonth(1, "E1", 20)
        Assert.False(_service.CloseMonth(2026, 10).Succeeded)
        Assert.Equal(0, _repo.ClosedKey)
    End Sub

    <Fact>
    Public Sub CloseMonth_Ready_Closes()
        AddMonth(1, "E1", 31)
        Assert.True(_service.CloseMonth(2026, 10).Succeeded)
        Assert.Equal(202610, _repo.ClosedKey)
    End Sub

    <Fact>
    Public Sub CloseMonth_WithoutApprove_IsRefused()
        AddMonth(1, "E1", 31)
        _perms.Allowed.Remove(PermissionAction.Approve)
        Assert.False(_service.CloseMonth(2026, 10).Succeeded)
        Assert.Equal(0, _repo.ClosedKey)
    End Sub

    <Fact>
    Public Sub CloseMonth_AlreadyClosed_IsRefused()
        AddMonth(1, "E1", 31)
        _repo.Locked.Add(202610)
        Assert.False(_service.CloseMonth(2026, 10).Succeeded)
    End Sub

    <Fact>
    Public Sub CloseMonth_MonthNotEnded_Blocks()
        _board.Month.Add(New MonthDayRow(1, "E1", "A", New DayRecord(New Date(2026, 11, 1), AttendanceStatus.Present, 0)))
        Assert.False(_service.CloseMonth(2026, 11).Succeeded)
    End Sub
End Class
