Imports Revolution.Modules.Attendance
Imports Xunit

Public Class MonthLockPolicyTests
    Private Shared Function Summary(unmarkedDays As Integer) As MonthSummary
        Dim records As New List(Of DayRecord)
        For d As Integer = 1 To 31 - unmarkedDays
            records.Add(New DayRecord(New Date(2026, 10, d), AttendanceStatus.Present, 0))
        Next
        Return MonthSummaryBuilder.Build(2026, 10, records).Value
    End Function

    <Fact>
    Public Sub KeyOf_IsYearAndMonth()
        Assert.Equal(202610, MonthLockPolicy.KeyOf(New Date(2026, 10, 31)))
    End Sub

    <Fact>
    Public Sub IsLocked_ChecksTheMonthSet()
        Dim locked As New HashSet(Of Integer) From {202609}
        Assert.True(MonthLockPolicy.IsLocked(New Date(2026, 9, 15), locked))
        Assert.False(MonthLockPolicy.IsLocked(New Date(2026, 10, 1), locked))
    End Sub

    <Fact>
    Public Sub CompleteEndedMonth_CanClose()
        Assert.True(MonthLockPolicy.CanClose(Summary(0), New Date(2026, 11, 1)).Succeeded)
    End Sub

    <Fact>
    Public Sub OnLastDayOfMonth_CannotClose()
        Dim r = MonthLockPolicy.CanClose(Summary(0), New Date(2026, 10, 31))
        Assert.False(r.Succeeded)
        Assert.Equal("The month has not ended yet.", r.Message)
    End Sub

    <Fact>
    Public Sub UnmarkedDays_BlockClose()
        Dim r = MonthLockPolicy.CanClose(Summary(2), New Date(2026, 11, 2))
        Assert.False(r.Succeeded)
        Assert.Equal("2 day(s) are not marked.", r.Message)
    End Sub

    <Theory>
    <InlineData("", False)>
    <InlineData("   ", False)>
    <InlineData("too short", False)>
    <InlineData("wrong shift entered for 3 days", True)>
    Public Sub ReopenReason_NeedsTenCharacters(reason As String, expected As Boolean)
        Assert.Equal(expected, MonthLockPolicy.ValidateReopenReason(reason).Succeeded)
    End Sub
End Class
