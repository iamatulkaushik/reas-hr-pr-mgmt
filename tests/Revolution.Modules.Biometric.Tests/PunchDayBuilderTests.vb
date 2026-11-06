Imports Revolution.Modules.Attendance
Imports Revolution.Modules.Biometric
Imports Xunit

Public Class PunchDayBuilderTests
    Private Shared ReadOnly Policy As New PunchWindowPolicy()
    Private Shared ReadOnly Monday As New Date(2026, 11, 2)

    Private Shared Function DayShift() As ShiftDefinition
        Return New ShiftDefinition("Day", TimeSpan.FromHours(8), TimeSpan.FromHours(17), 60, 300, 480, 240, OvertimeRounding.QuarterHour)
    End Function

    Private Shared Function NightShift() As ShiftDefinition
        Return New ShiftDefinition("Night", TimeSpan.FromHours(22), TimeSpan.FromHours(6), 30, 300, 420, 210, OvertimeRounding.QuarterHour)
    End Function

    Private Shared Function At(day As Date, hour As Integer, minute As Integer) As Date
        Return day.Date.AddHours(hour).AddMinutes(minute)
    End Function

    Private Shared Function Times(ParamArray values As Date()) As IReadOnlyList(Of Date)
        Return values.ToList()
    End Function

    <Fact>
    Public Sub TwoPunches_FirstIsInLastIsOut()
        Dim r As PunchDayResult = PunchDayBuilder.Build(DayShift(), Monday, Times(At(Monday, 7, 58), At(Monday, 17, 5)), Policy)
        Assert.Equal(At(Monday, 7, 58), r.PunchIn.Value)
        Assert.Equal(At(Monday, 17, 5), r.PunchOut.Value)
        Assert.False(r.AutoExit)
        Assert.Equal(AttendanceStatus.Present, r.Evaluation.Status)
        Assert.Equal(487, r.Evaluation.WorkedMinutes)
        Assert.True(PunchDayBuilder.IsMarkable(r))
    End Sub


    <Fact>
    Public Sub MiddlePunches_AreIgnored()
        Dim r As PunchDayResult = PunchDayBuilder.Build(DayShift(), Monday, Times(At(Monday, 8, 0), At(Monday, 12, 30), At(Monday, 13, 15), At(Monday, 19, 0)), Policy)
        Assert.Equal(At(Monday, 8, 0), r.PunchIn.Value)
        Assert.Equal(At(Monday, 19, 0), r.PunchOut.Value)
        Assert.Equal(120, r.Evaluation.OvertimeMinutes)
    End Sub

    <Fact>
    Public Sub OnePunch_ExitIsMarkedAtTheShiftEnd()
        Dim r As PunchDayResult = PunchDayBuilder.Build(DayShift(), Monday, Times(At(Monday, 8, 0)), Policy)
        Assert.True(r.AutoExit)
        Assert.Equal(At(Monday, 17, 0), r.PunchOut.Value)
        Assert.Equal(AttendanceStatus.Present, r.Evaluation.Status)
        Assert.Equal(480, r.Evaluation.WorkedMinutes)
        Assert.Equal(0, r.Evaluation.OvertimeMinutes)
    End Sub

    <Fact>
    Public Sub OnePunch_LateArrival_CountsLess_NotAFullDay()
        Dim r As PunchDayResult = PunchDayBuilder.Build(DayShift(), Monday, Times(At(Monday, 10, 0)), Policy)
        Assert.True(r.AutoExit)
        Assert.Equal(AttendanceStatus.HalfDay, r.Evaluation.Status)
        Assert.Equal(360, r.Evaluation.WorkedMinutes)
    End Sub

    <Fact>
    Public Sub DoublePunchAtTheGate_IsOnePunch()
        Dim r As PunchDayResult = PunchDayBuilder.Build(DayShift(), Monday, Times(At(Monday, 8, 0), At(Monday, 8, 2)), Policy)
        Assert.Equal(1, r.PunchCount)
        Assert.True(r.AutoExit)
    End Sub

    <Fact>
    Public Sub OnePunchAtOrAfterTheShiftEnd_IsLeftForReview()
        Dim r As PunchDayResult = PunchDayBuilder.Build(DayShift(), Monday, Times(At(Monday, 17, 30)), Policy)
        Assert.False(r.AutoExit)
        Assert.Null(r.Evaluation)
        Assert.False(PunchDayBuilder.IsMarkable(r))
        Assert.Contains("by hand", r.Note, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub NoPunchInTheWindow_GivesNothing()
        Dim r As PunchDayResult = PunchDayBuilder.Build(DayShift(), Monday, Times(At(Monday, 2, 0), At(Monday.AddDays(1), 6, 0)), Policy)
        Assert.Equal(0, r.PunchCount)
        Assert.Null(r.Evaluation)
        Assert.False(PunchDayBuilder.IsMarkable(r))
    End Sub

    <Fact>
    Public Sub NightShift_RunsIntoTheNextDay()
        Dim r As PunchDayResult = PunchDayBuilder.Build(NightShift(), Monday, Times(At(Monday, 21, 55), At(Monday.AddDays(1), 6, 10)), Policy)
        Assert.Equal(At(Monday, 21, 55), r.PunchIn.Value)
        Assert.Equal(At(Monday.AddDays(1), 6, 10), r.PunchOut.Value)
        Assert.Equal(AttendanceStatus.Present, r.Evaluation.Status)
        Assert.Equal(15, r.Evaluation.OvertimeMinutes)
    End Sub

    <Fact>
    Public Sub NightShift_OnePunch_ExitAtNextMorningShiftEnd()
        Dim r As PunchDayResult = PunchDayBuilder.Build(NightShift(), Monday, Times(At(Monday, 22, 5)), Policy)
        Assert.True(r.AutoExit)
        Assert.Equal(At(Monday.AddDays(1), 6, 0), r.PunchOut.Value)
        Assert.Equal(AttendanceStatus.Present, r.Evaluation.Status)
    End Sub

    <Fact>
    Public Sub Window_IsFromEarlyMarginToLateMarginAfterTheEnd()
        Dim w As Tuple(Of Date, Date) = PunchDayBuilder.Window(NightShift(), Monday, Policy)
        Assert.Equal(At(Monday, 19, 0), w.Item1)
        Assert.Equal(At(Monday.AddDays(1), 12, 0), w.Item2)
    End Sub

    <Fact>
    Public Sub Policy_RefusesNonsense()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New PunchWindowPolicy(-1))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New PunchWindowPolicy(60, 721))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New PunchWindowPolicy(60, 60, 0))
    End Sub
End Class
