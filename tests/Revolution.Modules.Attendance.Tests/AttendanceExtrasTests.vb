Imports Revolution.Core
Imports Revolution.Modules.Attendance
Imports Xunit

Public Class AttendanceExtrasTests
    Private ReadOnly _repo As New FakeAttendanceRepository()
    Private ReadOnly _board As New FakeBoardRepository()
    Private ReadOnly _perms As New FakePermissions()
    Private ReadOnly _service As AttendanceBoardService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Edit)
        _perms.Allowed.Add(PermissionAction.Delete)
        Dim today As Func(Of Date) = Function() New Date(2026, 11, 5)
        Dim attendance As New AttendanceService(_repo, _perms, New FakeEntitlements(), New FakeAudit(), New FakeSession(), today)
        _service = New AttendanceBoardService(_board, attendance, _repo, _perms, today)
        _board.Day.Add(New AttendanceSheetRow(1, "E1", "A", Nothing, Nothing, 0, 0))
        _board.Day.Add(New AttendanceSheetRow(2, "E2", "B", AttendanceStatus.Present, 3, 480, 0))
    End Sub

    Private Shared Function Row(code As String, day As String, status As String, Optional overtime As String = "") As ImportRow
        Return New ImportRow(2, New Dictionary(Of String, String) From {
            {AttendanceBulkTemplate.ColCode, code}, {AttendanceBulkTemplate.ColDate, day},
            {AttendanceBulkTemplate.ColStatus, status}, {AttendanceBulkTemplate.ColOvertime, overtime}})
    End Function

    <Fact>
    Public Sub Bulk_ValidRow_PassesAndSaves()
        Dim handler = _service.CreateBulkHandler()
        Dim line = Row("E1", "2026-11-03", "P", "1.5")
        Assert.Equal(String.Empty, handler.Validate(line))
        Assert.True(handler.Save(line).Succeeded)
        Assert.Equal(90, _repo.Saved.Single().OvertimeMinutes)
    End Sub

    <Fact>
    Public Sub Bulk_MarkedDay_IsRefused()
        Assert.Contains("already marked", _service.CreateBulkHandler().Validate(Row("E2", "2026-11-03", "P")))
    End Sub

    <Theory>
    <InlineData("E9", "2026-11-03", "P", "not on the roll")>
    <InlineData("", "2026-11-03", "P", "Emp Code")>
    <InlineData("E1", "not a date", "P", "Date")>
    <InlineData("E1", "2026-11-06", "P", "future")>
    <InlineData("E1", "2026-11-03", "Z", "Status")>
    <InlineData("E1", "2026-11-03", "P", "OT Hours")>
    Public Sub Bulk_BadRow_NamesTheColumn(code As String, day As String, status As String, expected As String)
        Dim overtime = If(expected = "OT Hours", "25", String.Empty)
        Dim message = _service.CreateBulkHandler().Validate(Row(code, day, status, overtime))
        Assert.Contains(expected, message)
    End Sub

    <Fact>
    Public Sub Bulk_MessageNeverRepeatsTheCell()
        Dim message = _service.CreateBulkHandler().Validate(Row("SECRET99", "2026-11-03", "P"))
        Assert.DoesNotContain("SECRET99", message)
    End Sub

    <Fact>
    Public Sub Bulk_SameCodeAndDay_HaveSameKey()
        Dim handler = _service.CreateBulkHandler()
        Assert.Equal(handler.RowKey(Row("E1", "2026-11-03", "P")), handler.RowKey(Row("E1", "03-11-2026", "A")))
    End Sub

    <Fact>
    Public Sub Bulk_WithoutEdit_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.NotEqual(String.Empty, _service.CreateBulkHandler().Validate(Row("E1", "2026-11-03", "P")))
    End Sub

    <Fact>
    Public Sub DeleteDays_RemovesEachTickedDay()
        Dim r = _service.DeleteDays(New Date(2026, 11, 3), New Integer() {1, 2})
        Assert.True(r.Succeeded)
        Assert.Equal(2, r.Value)
        Assert.Equal(2, _repo.Deleted.Count)
    End Sub

    <Fact>
    Public Sub DeleteDays_ClosedMonth_IsRefused()
        _repo.Locked.Add(202611)
        Dim r = _service.DeleteDays(New Date(2026, 11, 3), New Integer() {2})
        Assert.False(r.Succeeded)
        Assert.Contains("month is closed", r.Message)
        Assert.Empty(_repo.Deleted)
    End Sub

    <Fact>
    Public Sub DeleteDays_WithoutDeletePermission_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Delete)
        Assert.False(_service.DeleteDays(New Date(2026, 11, 3), New Integer() {2}).Succeeded)
        Assert.Empty(_repo.Deleted)
    End Sub

    <Fact>
    Public Sub DeleteDays_NothingTicked_IsRefused()
        Assert.False(_service.DeleteDays(New Date(2026, 11, 3), New Integer() {}).Succeeded)
    End Sub

    <Fact>
    Public Sub LoadEmployeeMonth_ReturnsMarkedDays()
        _board.EmployeeMonth.Add(New EmployeeDayRow(New Date(2026, 11, 2), AttendanceStatus.Present, 3, 480, 0))
        Dim r = _service.LoadEmployeeMonth(2, 2026, 11)
        Assert.True(r.Succeeded)
        Assert.Single(r.Value)
    End Sub

    <Fact>
    Public Sub LoadEmployeeMonth_BadMonth_IsRefused()
        Assert.False(_service.LoadEmployeeMonth(2, 2026, 13).Succeeded)
    End Sub

    <Fact>
    Public Sub SaveEmployeeMonth_SavesEachDay()
        Dim rows = New List(Of EmployeeDayRow) From {
            New EmployeeDayRow(New Date(2026, 11, 2), AttendanceStatus.Present, Nothing, 0, 0),
            New EmployeeDayRow(New Date(2026, 11, 3), AttendanceStatus.Absent, Nothing, 0, 0)}
        Assert.True(_service.SaveEmployeeMonth(1, rows).Succeeded)
        Assert.Equal(2, _repo.Saved.Count)
    End Sub
End Class
