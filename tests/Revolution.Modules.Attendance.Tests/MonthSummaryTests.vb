Imports Revolution.Core
Imports Revolution.Modules.Attendance
Imports Xunit

Public Class MonthSummaryTests
    Private Shared Function Marks(ParamArray items As Tuple(Of Integer, AttendanceStatus, Integer)()) As List(Of DayRecord)
        Dim list As New List(Of DayRecord)
        For Each t In items
            list.Add(New DayRecord(New Date(2026, 10, t.Item1), t.Item2, t.Item3))
        Next
        Return list
    End Function

    Private Shared Function FullOctober() As List(Of DayRecord)
        Dim list As New List(Of DayRecord)
        For d As Integer = 1 To 31
            Dim status As AttendanceStatus
            Select Case d
                Case 1 To 20 : status = AttendanceStatus.Present
                Case 21, 22 : status = AttendanceStatus.HalfDay
                Case 23 To 26 : status = AttendanceStatus.WeeklyOff
                Case 27 : status = AttendanceStatus.Holiday
                Case 28 : status = AttendanceStatus.PaidLeave
                Case Else : status = AttendanceStatus.Absent
            End Select
            list.Add(New DayRecord(New Date(2026, 10, d), status, If(d = 3, 90, If(d = 4, 45, 0))))
        Next
        Return list
    End Function

    <Fact>
    Public Sub FullMonth_CountsAndPaysCorrectly()
        Dim r = MonthSummaryBuilder.Build(2026, 10, FullOctober())
        Assert.True(r.Succeeded)
        Dim s = r.Value
        Assert.Equal(31, s.DaysInMonth)
        Assert.Equal(20, s.PresentDays)
        Assert.Equal(2, s.HalfDays)
        Assert.Equal(4, s.WeeklyOffDays)
        Assert.Equal(1, s.HolidayDays)
        Assert.Equal(1, s.PaidLeaveDays)
        Assert.Equal(3, s.AbsentDays)
        Assert.Equal(0, s.UnmarkedDays)
        Assert.Equal(27D, s.DaysPaid)
        Assert.Equal(4D, s.LopDays)
        Assert.Equal(31D, s.DaysPaid + s.LopDays + s.UnmarkedDays)
    End Sub

    <Fact>
    Public Sub Overtime_SumsMinutesAndConvertsToHours()
        Dim s = MonthSummaryBuilder.Build(2026, 10, FullOctober()).Value
        Assert.Equal(135, s.OvertimeMinutes)
        Assert.Equal(2.25D, s.OvertimeHours)
    End Sub

    <Fact>
    Public Sub MissingDays_AreUnmarkedNotLop()
        Dim s = MonthSummaryBuilder.Build(2026, 10, Marks(
            Tuple.Create(1, AttendanceStatus.Present, 0),
            Tuple.Create(2, AttendanceStatus.Absent, 0))).Value
        Assert.Equal(29, s.UnmarkedDays)
        Assert.Equal(1D, s.DaysPaid)
        Assert.Equal(1D, s.LopDays)
    End Sub

    <Fact>
    Public Sub DuplicateDay_Fails()
        Dim r = MonthSummaryBuilder.Build(2026, 10, Marks(
            Tuple.Create(5, AttendanceStatus.Present, 0),
            Tuple.Create(5, AttendanceStatus.Absent, 0)))
        Assert.False(r.Succeeded)
        Assert.Equal("05-10-2026 is marked twice.", r.Message)
    End Sub

    <Fact>
    Public Sub DayOutsideMonth_Fails()
        Dim records As New List(Of DayRecord) From {New DayRecord(New Date(2026, 11, 1), AttendanceStatus.Present, 0)}
        Dim r = MonthSummaryBuilder.Build(2026, 10, records)
        Assert.False(r.Succeeded)
        Assert.Equal("01-11-2026 is outside the month.", r.Message)
    End Sub

    <Fact>
    Public Sub BadMonth_Fails()
        Assert.False(MonthSummaryBuilder.Build(2026, 13, New List(Of DayRecord)).Succeeded)
        Assert.False(MonthSummaryBuilder.Build(2026, 0, New List(Of DayRecord)).Succeeded)
    End Sub

    <Fact>
    Public Sub LeapFebruary_Has29Days()
        Dim s = MonthSummaryBuilder.Build(2028, 2, New List(Of DayRecord)).Value
        Assert.Equal(29, s.DaysInMonth)
        Assert.Equal(29, s.UnmarkedDays)
    End Sub

    <Fact>
    Public Sub NegativeOvertime_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New DayRecord(New Date(2026, 10, 1), AttendanceStatus.Present, -1))
    End Sub
End Class
