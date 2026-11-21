Imports Revolution.Core
Imports Revolution.Modules.Attendance
Imports Xunit

Friend NotInheritable Class FakeMonthlyRepository
    Implements IAttendanceMonthlyRepository

    Public ReadOnly Rows As New List(Of MonthlyAttendanceRow)()
    Public ReadOnly Saved As New List(Of MonthlyAttendanceRow)()
    Public ReadOnly Deleted As New List(Of Integer)()
    Public Mode As AttendanceMode = AttendanceMode.Monthly
    Public FailWith As String

    Public Function ListMonth(periodStart As Date) As IReadOnlyList(Of MonthlyAttendanceRow) Implements IAttendanceMonthlyRepository.ListMonth
        Return Rows
    End Function

    Public Sub Save(periodStart As Date, row As MonthlyAttendanceRow, requireComplete As Boolean, userId As Integer?) Implements IAttendanceMonthlyRepository.Save
        If FailWith IsNot Nothing Then Throw New AttendanceRuleException(FailWith)
        Saved.Add(row)
        Rows.RemoveAll(Function(r) r.EmployeeId = row.EmployeeId)
        Rows.Add(row)
    End Sub

    Public Sub Delete(employeeId As Integer, periodStart As Date, userId As Integer?) Implements IAttendanceMonthlyRepository.Delete
        If FailWith IsNot Nothing Then Throw New AttendanceRuleException(FailWith)
        Deleted.Add(employeeId)
    End Sub

    Public Function GetMode() As AttendanceMode Implements IAttendanceMonthlyRepository.GetMode
        Return Mode
    End Function

    Public Sub SetMode(newMode As AttendanceMode) Implements IAttendanceMonthlyRepository.SetMode
        If FailWith IsNot Nothing Then Throw New AttendanceRuleException(FailWith)
        Mode = newMode
    End Sub
End Class

Public Class AttendanceMonthlyTests
    Private Shared ReadOnly October As New Date(2026, 10, 1)

    Private ReadOnly _repo As New FakeMonthlyRepository()
    Private ReadOnly _roster As New FakeRosterRepository()
    Private ReadOnly _attRepo As New FakeAttendanceRepository()
    Private ReadOnly _perms As New FakePermissions()
    Private ReadOnly _plan As New FakeEntitlements()
    Private ReadOnly _audit As New FakeAudit()
    Private ReadOnly _service As AttendanceMonthlyService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Edit)
        _perms.Allowed.Add(PermissionAction.Delete)
        _perms.Allowed.Add(PermissionAction.Approve)
        Dim today As Func(Of Date) = Function() New Date(2026, 11, 5)
        Dim attendance As New AttendanceService(_attRepo, _perms, _plan, _audit, New FakeSession(), today)
        _service = New AttendanceMonthlyService(_repo, _roster, attendance, _attRepo, _perms, _plan, _audit, New FakeSession(), today)
        _repo.Rows.Add(Person(101, "E001", New Date(2026, 10, 1), 31))
        _repo.Rows.Add(Person(102, "E002", New Date(2026, 10, 10), 22))
        _roster.Rules.Add(New WeeklyOffRule(New Date(2026, 1, 1), 64))
        _roster.SavedHolidays.Add(New HolidayItem(New Date(2026, 10, 2), "Gandhi Jayanti"))
        _roster.SavedHolidays.Add(New HolidayItem(New Date(2026, 10, 25), "Falls on a Sunday"))
    End Sub

    Private Shared Function Person(id As Integer, code As String, windowFrom As Date, windowDays As Integer) As MonthlyAttendanceRow
        Return New MonthlyAttendanceRow(id, code, "Name " & code, windowFrom, New Date(2026, 10, 31), windowDays, False, 0, 0, 0, 0, 0, 0, 0D, String.Empty)
    End Function

    Private Shared Function Row(values As IDictionary(Of String, String)) As ImportRow
        Return New ImportRow(2, New Dictionary(Of String, String)(values, StringComparer.OrdinalIgnoreCase))
    End Function

    Private Shared Function Sheet(code As String, present As String, Optional absent As String = "", Optional weeklyOff As String = "", Optional holiday As String = "",
                                  Optional overtime As String = "") As ImportRow
        Return Row(New Dictionary(Of String, String) From {
            {MonthlyAttendanceTemplate.ColCode, code}, {MonthlyAttendanceTemplate.ColPresent, present}, {MonthlyAttendanceTemplate.ColAbsent, absent},
            {MonthlyAttendanceTemplate.ColWeeklyOff, weeklyOff}, {MonthlyAttendanceTemplate.ColHoliday, holiday}, {MonthlyAttendanceTemplate.ColOvertime, overtime}})
    End Function

    Private Function Complete(code As String) As MonthlyAttendanceRow
        Dim p = _repo.Rows.First(Function(r) r.EmpCode = code)
        Return p.WithCounts(p.WindowDays, 0, 0, 0, 0, 0, 0D, String.Empty)
    End Function

    ' ---- service ----

    <Fact>
    Public Sub Save_NeedsEdit()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.Save(October, Complete("E001"), True).Succeeded)
        Assert.Empty(_repo.Saved)
    End Sub

    <Fact>
    Public Sub Save_Complete_IsStored_AndAudited()
        Assert.True(_service.Save(October, Complete("E001"), True).Succeeded)
        Assert.Single(_repo.Saved)
        Assert.Single(_audit.Entries)
    End Sub

    <Fact>
    Public Sub Save_Incomplete_IsStoredWhenNotRequired_AndRefusedWhenRequired()
        Dim part = _repo.Rows(0).WithCounts(20, 0, 4, 1, 0, 0, 0D, String.Empty)
        Assert.True(_service.Save(October, part, False).Succeeded)
        Dim r = _service.Save(October, part, True)
        Assert.False(r.Succeeded)
        Assert.Equal("Days add up to 25 of 31. Fill Absent Days or the missing days.", r.Message)
    End Sub

    <Fact>
    Public Sub Save_MoreThanTheWindow_IsRefused()
        Dim r = _service.Save(October, _repo.Rows(1).WithCounts(23, 0, 0, 0, 0, 0, 0D, String.Empty), False)
        Assert.False(r.Succeeded)
    End Sub

    <Fact>
    Public Sub Save_ClosedMonth_IsRefused()
        _attRepo.Locked.Add(202610)
        Dim r = _service.Save(October, Complete("E001"), True)
        Assert.False(r.Succeeded)
        Assert.Equal("The month is closed.", r.Message)
        Assert.Empty(_repo.Saved)
    End Sub

    <Fact>
    Public Sub Save_AMonthThatHasNotStarted_IsRefused()
        Assert.False(_service.Save(New Date(2026, 12, 1), Complete("E001"), True).Succeeded)
    End Sub

    <Fact>
    Public Sub Save_NotTheFirstOfTheMonth_IsRefused()
        Assert.False(_service.Save(New Date(2026, 10, 5), Complete("E001"), True).Succeeded)
    End Sub

    <Theory>
    <InlineData(0, True)>
    <InlineData(744, True)>
    <InlineData(745, False)>
    <InlineData(-1, False)>
    Public Sub Save_OvertimeLimits(hours As Decimal, expected As Boolean)
        Dim p = _repo.Rows(0)
        Dim r = _service.Save(October, p.WithCounts(p.WindowDays, 0, 0, 0, 0, 0, hours, String.Empty), True)
        Assert.Equal(expected, r.Succeeded)
    End Sub

    <Fact>
    Public Sub Save_RepositoryRuleBreak_BecomesAMessage()
        _repo.FailWith = "The employee is not employed in this month."
        Dim r = _service.Save(October, Complete("E001"), True)
        Assert.False(r.Succeeded)
        Assert.Equal("The employee is not employed in this month.", r.Message)
    End Sub

    <Fact>
    Public Sub Delete_NeedsDelete_AndAnOpenMonth()
        _perms.Allowed.Remove(PermissionAction.Delete)
        Assert.False(_service.Delete(101, October).Succeeded)
        _perms.Allowed.Add(PermissionAction.Delete)
        _attRepo.Locked.Add(202610)
        Assert.False(_service.Delete(101, October).Succeeded)
        _attRepo.Locked.Clear()
        Assert.True(_service.Delete(101, October).Succeeded)
        Assert.Equal(101, _repo.Deleted.Single())
    End Sub

    <Fact>
    Public Sub Mode_DailyNeedsThePlanFeature()
        _plan.DailyEnabled = False
        Dim r = _service.SetMode(AttendanceMode.Daily)
        Assert.False(r.Succeeded)
        Assert.Equal(AttendanceMode.Monthly, _repo.Mode)
        _plan.DailyEnabled = True
        Assert.True(_service.SetMode(AttendanceMode.Daily).Succeeded)
        Assert.Equal(AttendanceMode.Daily, _service.CurrentMode())
    End Sub

    <Fact>
    Public Sub CurrentMode_FallsBackToMonthly_WhenThePlanLosesDaily()
        _repo.Mode = AttendanceMode.Daily
        _plan.DailyEnabled = False
        Assert.Equal(AttendanceMode.Monthly, _service.CurrentMode())
    End Sub

    <Fact>
    Public Sub DefaultPeriod_IsThePreviousMonth()
        Assert.Equal(October, _service.DefaultPeriod())
    End Sub

    <Fact>
    Public Sub CloseSheet_BlocksOnAMissingRow_AndAnIncompleteRow()
        _repo.Rows(0) = Complete("E001")
        _repo.Rows(0) = _repo.Rows(0).WithCounts(31, 0, 0, 0, 0, 0, 0D, String.Empty)
        Dim sheet = _service.LoadCloseSheet(2026, 10)
        Assert.True(sheet.Succeeded)
        Assert.Single(sheet.Value.Blockers)
        Assert.Contains("E002", sheet.Value.Blockers(0), StringComparison.Ordinal)
        Assert.False(sheet.Value.CanClose)
        _repo.Rows(1) = _repo.Rows(1).WithCounts(10, 0, 0, 0, 0, 0, 0D, String.Empty)
        Assert.Contains("not marked", _service.LoadCloseSheet(2026, 10).Value.Blockers(0), StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub CloseMonth_ClosesWhenEveryEmployeeIsComplete()
        _repo.Rows(0) = Complete("E001")
        _repo.Rows(1) = Complete("E002")
        Assert.True(_service.CloseMonth(2026, 10).Succeeded)
        Assert.Equal(202610, _attRepo.ClosedKey)
    End Sub

    <Fact>
    Public Sub CloseMonth_RefusesWhileTheMonthIsRunning()
        Assert.False(_service.CloseMonth(2026, 11).Succeeded)
    End Sub

    ' ---- rules ----

    <Fact>
    Public Sub RuleDefaults_CountWeeklyOffAndHolidays_InTheEmploymentWindow()
        Dim first = _service.RuleDefaults(_repo.Rows(0))
        Assert.Equal(4, first.Value.WeeklyOffDays)
        Assert.Equal(1, first.Value.HolidayDays)
        Dim joiner = _service.RuleDefaults(_repo.Rows(1))
        Assert.Equal(3, joiner.Value.WeeklyOffDays)
        Assert.Equal(0, joiner.Value.HolidayDays)
    End Sub

    <Fact>
    Public Sub WeeklyOffRule_LatestRuleOnOrBeforeTheDayApplies()
        Dim rules = New List(Of WeeklyOffRule) From {New WeeklyOffRule(New Date(2026, 1, 1), 64), New WeeklyOffRule(New Date(2026, 10, 15), 32)}
        ' Sundays 4 and 11 under the first rule; Saturdays 17, 24 and 31 under the second.
        Assert.Equal(5, MonthlyRules.WeeklyOffDays(New Date(2026, 10, 1), New Date(2026, 10, 31), rules))
    End Sub

    <Fact>
    Public Sub NoWeeklyOffRule_MeansNoWeeklyOff()
        Assert.Equal(0, MonthlyRules.WeeklyOffDays(New Date(2026, 10, 1), New Date(2026, 10, 31), New List(Of WeeklyOffRule)()))
    End Sub

    ' ---- summary ----

    <Fact>
    Public Sub FromMonthly_BuildsTheSummaryOfARow()
        Dim p = _repo.Rows(0).WithCounts(20, 2, 4, 1, 1, 3, 12.5D, String.Empty)
        Dim s = MonthSummaryBuilder.FromMonthly(2026, 10, p).Value
        Assert.Equal(20, s.PresentDays)
        Assert.Equal(2, s.HalfDays)
        Assert.Equal(3, s.AbsentDays)
        Assert.Equal(750, s.OvertimeMinutes)
        Assert.Equal(0, s.UnmarkedDays)
        Assert.Equal(27D, s.DaysPaid)
        Assert.Equal(4D, s.LopDays)
    End Sub

    <Fact>
    Public Sub FromMonthly_UnmarkedIsWhatTheCountsDoNotCover()
        Dim s = MonthSummaryBuilder.FromMonthly(2026, 10, _repo.Rows(1).WithCounts(10, 0, 0, 0, 0, 0, 0D, String.Empty)).Value
        Assert.Equal(12, s.UnmarkedDays)
        Assert.Equal(22, MonthSummaryBuilder.FromMonthly(2026, 10, _repo.Rows(1)).Value.UnmarkedDays)
    End Sub

    ' ---- error map and template ----

    <Theory>
    <InlineData(53601)>
    <InlineData(53602)>
    <InlineData(53603)>
    <InlineData(53604)>
    <InlineData(53605)>
    Public Sub ErrorMap_TranslatesTheMonthlyRuleErrors(number As Integer)
        Assert.Equal("text", AttendanceErrorMap.Translate(number, "text"))
    End Sub

    <Fact>
    Public Sub ErrorMap_MonthlyEmployeeKey_IsEmployeeNotFound()
        Assert.Equal("Employee not found.", AttendanceErrorMap.Translate(547, "conflict with FK_AttendanceMonthly_Employee"))
    End Sub

    <Fact>
    Public Sub Template_HasTheColumnsOfThePlan()
        Dim names = AttendanceMonthlyService.BulkTemplate().Columns.Select(Function(c) c.Name).ToList()
        Assert.Equal({"Emp Code", "Present Days", "Half Days", "Weekly Off Days", "Holiday Days", "Paid Leave Days", "Absent Days", "OT Hours", "Employee Name"}, names)
        Assert.Equal(2, AttendanceMonthlyService.BulkTemplate().Columns.Count(Function(c) c.Required))
    End Sub

    ' ---- handler ----

    <Fact>
    Public Sub Handler_BlankWeeklyOffAndHoliday_TakeTheCompanyRules()
        Dim h = _service.CreateBulkHandler(October)
        Dim line = Sheet("E001", "26")
        Assert.Equal(String.Empty, h.Validate(line))
        Assert.True(h.Save(line).Succeeded)
        Dim saved = _repo.Saved.Single()
        Assert.Equal(4, saved.WeeklyOffDays)
        Assert.Equal(1, saved.HolidayDays)
        Assert.Equal(26, saved.PresentDays)
    End Sub

    <Fact>
    Public Sub Handler_ExplicitZero_IsNotReplacedByTheRule()
        Dim h = _service.CreateBulkHandler(October)
        Assert.Equal("Days add up to 26 of 31. Fill Absent Days or the missing days.", h.Validate(Sheet("E001", "26", weeklyOff:="0", holiday:="0")))
    End Sub

    <Fact>
    Public Sub Handler_CountsThatDoNotAddUp_SayHowFarTheyAre()
        Dim h = _service.CreateBulkHandler(October)
        Assert.Equal("Days add up to 24 of 31. Fill Absent Days or the missing days.", h.Validate(Sheet("E001", "19")))
        Assert.Equal(String.Empty, h.Validate(Sheet("E001", "19", absent:="7")))
    End Sub

    <Fact>
    Public Sub Handler_MoreDaysThanEmployed_AreRefused()
        Dim h = _service.CreateBulkHandler(October)
        Assert.Contains("worked 22 days", h.Validate(Sheet("E002", "30")), StringComparison.Ordinal)
    End Sub

    <Theory>
    <InlineData("", "26", "Emp Code is empty.")>
    <InlineData("E999", "26", "Emp Code is not on the roll in this month.")>
    <InlineData("E001", "", "Present Days is empty.")>
    <InlineData("E001", "x", "Present Days is not a whole number.")>
    <InlineData("E001", "40", "Present Days must be between 0 and 31.")>
    <InlineData("E001", "-1", "Present Days must be between 0 and 31.")>
    Public Sub Handler_Messages_NameTheColumn(code As String, present As String, expected As String)
        Dim h = _service.CreateBulkHandler(October)
        Assert.Equal(expected, h.Validate(Sheet(code, present)))
    End Sub

    <Theory>
    <InlineData("abc", "OT Hours is not a number.")>
    <InlineData("745", "OT Hours must be between 0 and 744.")>
    <InlineData("1.234", "OT Hours can have two decimals at most.")>
    Public Sub Handler_Overtime_Messages(hours As String, expected As String)
        Dim h = _service.CreateBulkHandler(October)
        Assert.Equal(expected, h.Validate(Sheet("E001", "26", overtime:=hours)))
    End Sub

    <Fact>
    Public Sub Handler_ClosedMonth_IsRefused_AndNothingIsSaved()
        _attRepo.Locked.Add(202610)
        Dim h = _service.CreateBulkHandler(October)
        Assert.Equal("The month is closed.", h.Validate(Sheet("E001", "26")))
        Assert.False(h.Save(Sheet("E001", "26")).Succeeded)
        Assert.Empty(_repo.Saved)
    End Sub

    <Fact>
    Public Sub Handler_NoEditPermission_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Dim h = _service.CreateBulkHandler(October)
        Assert.Equal("You are not allowed to mark attendance.", h.Validate(Sheet("E001", "26")))
    End Sub

    <Fact>
    Public Sub Handler_TheSameSheetTwice_ReplacesTheRows()
        Dim h = _service.CreateBulkHandler(October)
        Assert.True(h.Save(Sheet("E001", "26")).Succeeded)
        Assert.True(h.Save(Sheet("E001", "26", overtime:="3.5")).Succeeded)
        Assert.Equal(2, _repo.Saved.Count)
        Assert.Equal(1, _repo.Rows.Count(Function(r) r.EmpCode = "E001"))
        Assert.Equal(3.5D, _repo.Rows.First(Function(r) r.EmpCode = "E001").OvertimeHours)
    End Sub

    <Fact>
    Public Sub Handler_RowKey_IsTheEmployeeCode_SoARepeatInTheListIsCaught()
        Dim h = _service.CreateBulkHandler(October)
        Assert.Equal("E001", h.RowKey(Sheet("E001", "26")))
        Assert.Equal(String.Empty, h.RowKey(Sheet("", "26")))
    End Sub

    <Fact>
    Public Sub Handler_EmployeeNotInTheSheet_IsReported()
        Dim h = _service.CreateBulkHandler(October)
        Assert.Equal(String.Empty, h.Validate(Sheet("E001", "26")))
        Dim total As Integer
        Dim missing = h.MissingFromSheet(5, total)
        Assert.Equal(1, total)
        Assert.Equal("E002", missing.Single())
    End Sub

    <Fact>
    Public Sub Handler_MissingList_IsLimited_ButTheTotalIsComplete()
        Dim h = _service.CreateBulkHandler(October)
        Dim total As Integer
        Dim missing = h.MissingFromSheet(1, total)
        Assert.Equal(2, total)
        Assert.Single(missing)
    End Sub
End Class
