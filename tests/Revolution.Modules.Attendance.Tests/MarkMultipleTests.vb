Imports Revolution.Core
Imports Revolution.Modules.Attendance
Imports Xunit

Public Class MarkMultipleTests
    Private ReadOnly _repo As New FakeAttendanceRepository()
    Private ReadOnly _board As New FakeBoardRepository()
    Private ReadOnly _perms As New FakePermissions()
    Private ReadOnly _service As AttendanceBoardService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Edit)
        Dim today As Func(Of Date) = Function() New Date(2026, 11, 5)
        Dim attendance As New AttendanceService(_repo, _perms, New FakeEntitlements(), New FakeAudit(), New FakeSession(), today)
        _service = New AttendanceBoardService(_board, attendance, _repo, _perms, today)
        _board.Day.Add(New AttendanceSheetRow(1, "E1", "A", Nothing, Nothing, 0, 0))
        _board.Day.Add(New AttendanceSheetRow(2, "E2", "B", Nothing, Nothing, 0, 0))
    End Sub

    Private Function Mark(ids As IEnumerable(Of Integer), Optional overwrite As Boolean = False,
                          Optional fromDay As Integer = 3, Optional toDay As Integer = 4) As OperationResult(Of Integer)
        Return _service.MarkMultiple(New Date(2026, 11, fromDay), New Date(2026, 11, toDay), ids, AttendanceStatus.Present, 0, overwrite)
    End Function

    <Fact>
    Public Sub UnmarkedDays_SaveEveryEmployeeAndDay()
        Dim r = Mark(New Integer() {1, 2})
        Assert.True(r.Succeeded)
        Assert.Equal(4, r.Value)
        Assert.Equal(4, _repo.Saved.Count)
    End Sub

    <Fact>
    Public Sub MarkedDay_StaysUnlessOverwrite()
        _board.Day(0) = New AttendanceSheetRow(1, "E1", "A", AttendanceStatus.Absent, Nothing, 0, 0)
        Assert.Equal(0, Mark(New Integer() {1}).Value)
        Assert.Empty(_repo.Saved)
        Assert.Equal(2, Mark(New Integer() {1}, overwrite:=True).Value)
    End Sub

    <Fact>
    Public Sub EmployeeNotOnRoll_IsSkipped()
        Dim r = Mark(New Integer() {99})
        Assert.True(r.Succeeded)
        Assert.Equal(0, r.Value)
    End Sub

    <Fact>
    Public Sub ClosedMonth_IsReportedOncePerMonth()
        _repo.Locked.Add(202611)
        Dim r = Mark(New Integer() {1, 2})
        Assert.False(r.Succeeded)
        Assert.Contains("0 saved, 1 not saved", r.Message)
        Assert.Contains("11-2026: The month is closed.", r.Message)
        Assert.Empty(_repo.Saved)
    End Sub

    <Fact>
    Public Sub FutureDay_IsRefused()
        Assert.False(Mark(New Integer() {1}, toDay:=6).Succeeded)
    End Sub

    <Fact>
    Public Sub FromAfterTo_IsRefused()
        Assert.False(Mark(New Integer() {1}, fromDay:=4, toDay:=3).Succeeded)
    End Sub

    <Fact>
    Public Sub NoEmployees_IsRefused()
        Assert.False(Mark(New Integer() {}).Succeeded)
    End Sub

    <Fact>
    Public Sub MoreThan31Days_IsRefused()
        Dim r = _service.MarkMultiple(New Date(2026, 10, 1), New Date(2026, 11, 3), New Integer() {1}, AttendanceStatus.Present, 0, False)
        Assert.False(r.Succeeded)
        Assert.Contains("31", r.Message)
    End Sub

    <Fact>
    Public Sub WithoutEdit_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(Mark(New Integer() {1}).Succeeded)
        Assert.Empty(_repo.Saved)
    End Sub
End Class
