Imports Revolution.Modules.Attendance

''' <summary>
''' What the punches of one shift occurrence give. Evaluation is Nothing when there was no punch in the window. AutoExit is true when only one punch was found and the exit was set to the end
''' of the assigned shift (Owner, 2026-11-04). When Evaluation.NeedsReview is true nobody should be marked from it.
''' </summary>
Public NotInheritable Class PunchDayResult
    Public Sub New(workDate As Date, punchIn As Date?, punchOut As Date?, autoExit As Boolean, punchCount As Integer, evaluation As DayEvaluation, note As String)
        Me.WorkDate = workDate
        Me.PunchIn = punchIn
        Me.PunchOut = punchOut
        Me.AutoExit = autoExit
        Me.PunchCount = punchCount
        Me.Evaluation = evaluation
        Me.Note = note
    End Sub

    Public ReadOnly Property WorkDate As Date
    Public ReadOnly Property PunchIn As Date?
    Public ReadOnly Property PunchOut As Date?
    Public ReadOnly Property AutoExit As Boolean
    ''' <summary>Punches in the window after the ones too close together were counted once.</summary>
    Public ReadOnly Property PunchCount As Integer
    Public ReadOnly Property Evaluation As DayEvaluation
    Public ReadOnly Property Note As String
End Class
