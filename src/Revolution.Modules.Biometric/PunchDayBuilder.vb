Imports Revolution.Modules.Attendance

''' <summary>
''' Turns the punches of one employee around one assigned shift into the in and out the attendance engine judges. The first punch in the window is the in. The last is the out. When only one
''' punch is found the exit is marked automatically at the end of the assigned shift (Owner, 2026-11-04), so a late arrival still counts less and nothing is paid for time not worked.
''' A single punch at or after the shift end is not enough to say anything and is left for review. Middle punches are ignored. A night shift runs to the next day. Pure.
''' </summary>
Public NotInheritable Class PunchDayBuilder
    Private Sub New()
    End Sub

    ''' <param name="workDate">The day the shift starts.</param>
    ''' <param name="punchTimes">Any punches of the employee; those outside this shift's window are ignored.</param>
    Public Shared Function Build(shift As ShiftDefinition, workDate As Date, punchTimes As IReadOnlyList(Of Date), policy As PunchWindowPolicy) As PunchDayResult
        If shift Is Nothing Then Throw New ArgumentNullException(NameOf(shift))
        If punchTimes Is Nothing Then Throw New ArgumentNullException(NameOf(punchTimes))
        If policy Is Nothing Then Throw New ArgumentNullException(NameOf(policy))
        Dim shiftStart As Date = workDate.Date + shift.StartTime
        Dim shiftEnd As Date = shiftStart.AddMinutes(shift.SpanMinutes)
        Dim span As Tuple(Of Date, Date) = Window(shift, workDate, policy)
        Dim lo As Date = span.Item1
        Dim hi As Date = span.Item2
        Dim kept As New List(Of Date)()
        For Each t As Date In punchTimes.Where(Function(p As Date) p >= lo AndAlso p <= hi).OrderBy(Function(p As Date) p)
            If kept.Count = 0 OrElse (t - kept(kept.Count - 1)).TotalMinutes >= policy.MinGapMinutes Then kept.Add(t)
        Next
        If kept.Count = 0 Then Return New PunchDayResult(workDate.Date, Nothing, Nothing, False, 0, Nothing, "No punch in the shift window.")
        If kept.Count = 1 Then Return OnePunch(shift, workDate.Date, kept(0), shiftEnd)
        Return New PunchDayResult(workDate.Date, kept(0), kept(kept.Count - 1), False, kept.Count, AttendanceEngine.Evaluate(shift, kept(0), kept(kept.Count - 1)), String.Empty)
    End Function

    ''' <summary>The first and last moment of the shift window for the shift starting on workDate.</summary>
    Public Shared Function Window(shift As ShiftDefinition, workDate As Date, policy As PunchWindowPolicy) As Tuple(Of Date, Date)
        Dim shiftStart As Date = workDate.Date + shift.StartTime
        Return Tuple.Create(shiftStart.AddMinutes(-policy.EarlyMinutes), shiftStart.AddMinutes(shift.SpanMinutes + policy.LateMinutes))
    End Function

    ''' <summary>True when the result can be marked: it has a judged day that does not need review.</summary>
    Public Shared Function IsMarkable(result As PunchDayResult) As Boolean
        Return result IsNot Nothing AndAlso result.Evaluation IsNot Nothing AndAlso Not result.Evaluation.NeedsReview
    End Function

    Private Shared Function OnePunch(shift As ShiftDefinition, workDate As Date, punch As Date, shiftEnd As Date) As PunchDayResult
        If punch >= shiftEnd Then
            Return New PunchDayResult(workDate, punch, Nothing, False, 1, Nothing, "Only one punch, at or after the shift end. Enter the day by hand.")
        End If
        Return New PunchDayResult(workDate, punch, shiftEnd, True, 1, AttendanceEngine.Evaluate(shift, punch, shiftEnd), "Exit marked at the end of the shift.")
    End Function
End Class
