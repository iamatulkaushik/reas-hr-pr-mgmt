''' <summary>Turns in and out punches into a day status and overtime. Working days only: weekly off and holidays are entered, not judged.</summary>
Public NotInheritable Class AttendanceEngine
    Private Const MaximumSpanMinutes As Integer = 1440

    Private Sub New()
    End Sub

    Public Shared Function Evaluate(shift As ShiftDefinition, punchIn As DateTime?, punchOut As DateTime?) As DayEvaluation
        If shift Is Nothing Then
            Throw New ArgumentNullException(NameOf(shift))
        End If
        If Not punchIn.HasValue OrElse Not punchOut.HasValue Then
            Return DayEvaluation.Review("A punch is missing.")
        End If
        If punchOut.Value <= punchIn.Value Then
            Return DayEvaluation.Review("Out time is not after in time.")
        End If
        Dim span As Integer = CInt(Math.Floor((punchOut.Value - punchIn.Value).TotalMinutes))
        If span > MaximumSpanMinutes Then
            Return DayEvaluation.Review("Punches are more than 24 hours apart.")
        End If
        Dim worked As Integer = WorkedMinutes(shift, span)
        Dim status As AttendanceStatus = StatusFor(shift, worked)
        Dim overtime As Integer = If(status = AttendanceStatus.Present, OvertimeFor(shift, worked), 0)
        Return DayEvaluation.Judged(status, worked, overtime)
    End Function

    Private Shared Function WorkedMinutes(shift As ShiftDefinition, span As Integer) As Integer
        If span > shift.BreakAppliesAfterMinutes Then
            Return Math.Max(0, span - shift.UnpaidBreakMinutes)
        End If
        Return span
    End Function

    Private Shared Function StatusFor(shift As ShiftDefinition, worked As Integer) As AttendanceStatus
        If worked >= shift.FullDayMinutes Then
            Return AttendanceStatus.Present
        End If
        If worked >= shift.HalfDayMinutes Then
            Return AttendanceStatus.HalfDay
        End If
        Return AttendanceStatus.Absent
    End Function

    Private Shared Function OvertimeFor(shift As ShiftDefinition, worked As Integer) As Integer
        Dim extra As Integer = Math.Max(0, worked - shift.ScheduledMinutes)
        Dim stepMinutes As Integer = CInt(shift.OvertimeStep)
        Return extra - (extra Mod stepMinutes)
    End Function
End Class
