Imports Revolution.Modules.Attendance
Imports Xunit

Public Class ShiftDefinitionTests
    Private Shared Function Day9To6() As ShiftDefinition
        Return New ShiftDefinition("General", TimeSpan.FromHours(9), TimeSpan.FromHours(18), 60, 300, 450, 240, OvertimeRounding.QuarterHour)
    End Function

    <Fact>
    Public Sub DayShift_ScheduledMinutesExcludeBreak()
        Assert.Equal(540, Day9To6().SpanMinutes)
        Assert.Equal(480, Day9To6().ScheduledMinutes)
    End Sub

    <Fact>
    Public Sub NightShift_SpansMidnight()
        Dim s = New ShiftDefinition("Night", TimeSpan.FromHours(22), TimeSpan.FromHours(6), 60, 300, 400, 200, OvertimeRounding.None)
        Assert.Equal(480, s.SpanMinutes)
        Assert.Equal(420, s.ScheduledMinutes)
    End Sub

    <Fact>
    Public Sub BlankName_Throws()
        Assert.Throws(Of ArgumentException)(Function() New ShiftDefinition(" ", TimeSpan.FromHours(9), TimeSpan.FromHours(18), 60, 300, 450, 240, OvertimeRounding.None))
    End Sub

    <Fact>
    Public Sub SameStartAndEnd_Throws()
        Assert.Throws(Of ArgumentException)(Function() New ShiftDefinition("X", TimeSpan.FromHours(9), TimeSpan.FromHours(9), 0, 300, 450, 240, OvertimeRounding.None))
    End Sub

    <Fact>
    Public Sub HalfDayNotBelowFullDay_Throws()
        Assert.Throws(Of ArgumentException)(Function() New ShiftDefinition("X", TimeSpan.FromHours(9), TimeSpan.FromHours(18), 60, 300, 240, 240, OvertimeRounding.None))
    End Sub

    <Fact>
    Public Sub FullDayAbovePaidMinutes_Throws()
        Assert.Throws(Of ArgumentException)(Function() New ShiftDefinition("X", TimeSpan.FromHours(9), TimeSpan.FromHours(18), 60, 300, 500, 240, OvertimeRounding.None))
    End Sub

    <Fact>
    Public Sub BreakAsLongAsShift_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New ShiftDefinition("X", TimeSpan.FromHours(9), TimeSpan.FromHours(10), 60, 30, 30, 10, OvertimeRounding.None))
    End Sub
End Class
