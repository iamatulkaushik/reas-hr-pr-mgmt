Imports Revolution.Core
Imports Revolution.Modules.Attendance
Imports Xunit

Public Class RosterServiceTests
    Private ReadOnly _repo As New FakeRosterRepository()
    Private ReadOnly _perms As New FakePermissions()
    Private ReadOnly _plan As New FakeEntitlements()
    Private ReadOnly _audit As New FakeAudit()
    Private ReadOnly _service As RosterService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Edit)
        _service = New RosterService(_repo, _perms, _plan, _audit, Function() New Date(2026, 11, 5))
    End Sub

    Private Shared Function DayShift(Optional name As String = "Day") As ShiftItem
        Return New ShiftItem(Nothing, name, TimeSpan.FromHours(9), TimeSpan.FromHours(18), 60, 300, 480, 240, OvertimeRounding.HalfHour, True)
    End Function

    <Fact>
    Public Sub SaveShift_Valid_SavesAndAudits()
        Dim r = _service.SaveShift(DayShift())
        Assert.True(r.Succeeded)
        Assert.Equal(11, r.Value)
        Assert.Equal("CreateShift", _audit.Entries(0).Action)
    End Sub

    <Fact>
    Public Sub SaveShift_WithoutEdit_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.SaveShift(DayShift()).Succeeded)
        Assert.Empty(_repo.SavedShifts)
    End Sub

    <Fact>
    Public Sub SaveShift_PlanWithoutShifts_IsRefused()
        _plan.ShiftOtEnabled = False
        Assert.False(_service.SaveShift(DayShift()).Succeeded)
        Assert.False(_service.ListShifts().Succeeded)
    End Sub

    <Fact>
    Public Sub SaveShift_HalfDayAboveFullDay_IsRefused()
        Dim bad As New ShiftItem(Nothing, "Bad", TimeSpan.FromHours(9), TimeSpan.FromHours(18), 60, 300, 480, 480, OvertimeRounding.Hour, True)
        Dim r = _service.SaveShift(bad)
        Assert.False(r.Succeeded)
        Assert.Empty(_repo.SavedShifts)
    End Sub

    <Fact>
    Public Sub SaveShift_BlankName_IsRefused()
        Assert.False(_service.SaveShift(DayShift("  ")).Succeeded)
    End Sub

    <Fact>
    Public Sub SaveHoliday_Valid_Saves()
        Assert.True(_service.SaveHoliday(New Date(2026, 11, 8), "  Diwali ").Succeeded)
        Assert.Equal("Diwali", _repo.SavedHolidays.Single().Name)
    End Sub

    <Fact>
    Public Sub SaveHoliday_BlankName_IsRefused()
        Assert.False(_service.SaveHoliday(New Date(2026, 11, 8), "").Succeeded)
    End Sub

    <Theory>
    <InlineData(-1)>
    <InlineData(128)>
    Public Sub SaveWeeklyOff_BadMask_IsRefused(mask As Integer)
        Assert.False(_service.SaveWeeklyOff(New Date(2026, 4, 1), mask).Succeeded)
    End Sub

    <Fact>
    Public Sub AssignShift_DatabaseRule_ReturnsItsMessage()
        _repo.FailWith = "The new shift must start after the current one."
        Dim r = _service.AssignShift(5, 11, New Date(2026, 11, 1))
        Assert.False(r.Succeeded)
        Assert.Equal(_repo.FailWith, r.Message)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub AssignShift_Valid_AssignsAndAudits()
        Assert.True(_service.AssignShift(5, 11, New Date(2026, 11, 1)).Succeeded)
        Assert.Equal("5:11:20261101", _repo.Assigned.Single())
        Assert.Equal("AssignShift", _audit.Entries(0).Action)
    End Sub

    <Fact>
    Public Sub WeeklyOffRule_MaskStartsOnMonday()
        Dim rule As New WeeklyOffRule(New Date(2026, 4, 1), 64)
        Assert.True(rule.IsOff(DayOfWeek.Sunday))
        Assert.False(rule.IsOff(DayOfWeek.Monday))
        Assert.True(New WeeklyOffRule(New Date(2026, 4, 1), 1).IsOff(DayOfWeek.Monday))
    End Sub

    <Theory>
    <InlineData(50020, True)>
    <InlineData(50021, True)>
    <InlineData(50002, False)>
    Public Sub RosterErrorMap_MapsRulesOnly(number As Integer, mapped As Boolean)
        Assert.Equal(mapped, RosterErrorMap.Translate(number, "msg") IsNot Nothing)
    End Sub
End Class
