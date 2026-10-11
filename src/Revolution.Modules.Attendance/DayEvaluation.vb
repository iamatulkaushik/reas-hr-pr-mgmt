''' <summary>Result of judging one day from its punches. NeedsReview means a person must decide.</summary>
Public NotInheritable Class DayEvaluation
    Private Sub New(status As AttendanceStatus, workedMinutes As Integer, overtimeMinutes As Integer, needsReview As Boolean, message As String)
        Me.Status = status
        Me.WorkedMinutes = workedMinutes
        Me.OvertimeMinutes = overtimeMinutes
        Me.NeedsReview = needsReview
        Me.Message = message
    End Sub

    Public ReadOnly Property Status As AttendanceStatus
    Public ReadOnly Property WorkedMinutes As Integer
    Public ReadOnly Property OvertimeMinutes As Integer
    Public ReadOnly Property NeedsReview As Boolean
    Public ReadOnly Property Message As String

    Friend Shared Function Judged(status As AttendanceStatus, workedMinutes As Integer, overtimeMinutes As Integer) As DayEvaluation
        Return New DayEvaluation(status, workedMinutes, overtimeMinutes, False, String.Empty)
    End Function

    Friend Shared Function Review(message As String) As DayEvaluation
        Return New DayEvaluation(AttendanceStatus.Absent, 0, 0, True, message)
    End Function
End Class
