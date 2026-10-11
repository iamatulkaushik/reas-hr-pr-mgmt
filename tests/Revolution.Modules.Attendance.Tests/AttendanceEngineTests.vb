Imports Revolution.Modules.Attendance
Imports Xunit

Public Class AttendanceEngineTests
    Private Shared ReadOnly Day As New Date(2026, 10, 5)

    Private Shared Function General(Optional stepSize As OvertimeRounding = OvertimeRounding.QuarterHour) As ShiftDefinition
        Return New ShiftDefinition("General", TimeSpan.FromHours(9), TimeSpan.FromHours(18), 60, 300, 450, 240, stepSize)
    End Function

    Private Shared Function At(hour As Integer, minute As Integer) As DateTime
        Return Day.AddHours(hour).AddMinutes(minute)
    End Function

    <Fact>
    Public Sub FullShift_IsPresentWithNoOvertime()
        Dim r = AttendanceEngine.Evaluate(General(), At(9, 0), At(18, 0))
        Assert.Equal(AttendanceStatus.Present, r.Status)
        Assert.Equal(480, r.WorkedMinutes)
        Assert.Equal(0, r.OvertimeMinutes)
        Assert.False(r.NeedsReview)
    End Sub

    <Fact>
    Public Sub OvertimeIsOnlyAboveScheduledMinutes()
        Dim r = AttendanceEngine.Evaluate(General(), At(9, 0), At(19, 30))
        Assert.Equal(570, r.WorkedMinutes)
        Assert.Equal(90, r.OvertimeMinutes)
    End Sub

    <Fact>
    Public Sub OvertimeRoundsDownToCompanyStep()
        Dim r = AttendanceEngine.Evaluate(General(), At(9, 0), At(19, 35))
        Assert.Equal(90, r.OvertimeMinutes)
    End Sub

    <Fact>
    Public Sub NoRounding_KeepsEveryMinute()
        Dim r = AttendanceEngine.Evaluate(General(OvertimeRounding.None), At(9, 0), At(19, 35))
        Assert.Equal(95, r.OvertimeMinutes)
    End Sub

    <Fact>
    Public Sub ShortStayWithoutBreak_IsHalfDay()
        Dim r = AttendanceEngine.Evaluate(General(), At(9, 0), At(13, 30))
        Assert.Equal(AttendanceStatus.HalfDay, r.Status)
        Assert.Equal(270, r.WorkedMinutes)
        Assert.Equal(0, r.OvertimeMinutes)
    End Sub

    <Fact>
    Public Sub SixAndHalfHoursNet_IsHalfDay()
        Dim r = AttendanceEngine.Evaluate(General(), At(9, 0), At(16, 30))
        Assert.Equal(390, r.WorkedMinutes)
        Assert.Equal(AttendanceStatus.HalfDay, r.Status)
    End Sub

    <Fact>
    Public Sub ExactlyFullDayMinutes_IsPresent()
        Dim r = AttendanceEngine.Evaluate(General(), At(9, 0), At(17, 30))
        Assert.Equal(450, r.WorkedMinutes)
        Assert.Equal(AttendanceStatus.Present, r.Status)
    End Sub

    <Fact>
    Public Sub ThreeHours_IsAbsent()
        Dim r = AttendanceEngine.Evaluate(General(), At(9, 0), At(12, 0))
        Assert.Equal(AttendanceStatus.Absent, r.Status)
        Assert.False(r.NeedsReview)
    End Sub

    <Fact>
    Public Sub NightShift_CrossesMidnight()
        Dim night = New ShiftDefinition("Night", TimeSpan.FromHours(22), TimeSpan.FromHours(6), 60, 300, 400, 200, OvertimeRounding.None)
        Dim r = AttendanceEngine.Evaluate(night, Day.AddHours(22), Day.AddDays(1).AddHours(6))
        Assert.Equal(AttendanceStatus.Present, r.Status)
        Assert.Equal(420, r.WorkedMinutes)
        Assert.Equal(0, r.OvertimeMinutes)
    End Sub

    <Fact>
    Public Sub MissingOutPunch_NeedsReview()
        Dim r = AttendanceEngine.Evaluate(General(), At(9, 0), Nothing)
        Assert.True(r.NeedsReview)
        Assert.Equal("A punch is missing.", r.Message)
    End Sub

    <Fact>
    Public Sub OutBeforeIn_NeedsReview()
        Dim r = AttendanceEngine.Evaluate(General(), At(18, 0), At(9, 0))
        Assert.True(r.NeedsReview)
    End Sub

    <Fact>
    Public Sub MoreThanDayApart_NeedsReview()
        Dim r = AttendanceEngine.Evaluate(General(), At(9, 0), Day.AddDays(1).AddHours(10))
        Assert.True(r.NeedsReview)
    End Sub

    <Fact>
    Public Sub NullShift_Throws()
        Assert.Throws(Of ArgumentNullException)(Function() AttendanceEngine.Evaluate(Nothing, At(9, 0), At(18, 0)))
    End Sub
End Class
