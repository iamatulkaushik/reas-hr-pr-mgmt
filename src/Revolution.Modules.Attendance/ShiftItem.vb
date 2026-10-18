''' <summary>A shift as stored (att.Shift). Times are clock times; an end at or before the start means the next day.</summary>
Public NotInheritable Class ShiftItem
    Public Sub New(shiftId As Integer?, name As String, startTime As TimeSpan, endTime As TimeSpan, unpaidBreakMinutes As Integer,
                   breakAppliesAfterMinutes As Integer, fullDayMinutes As Integer, halfDayMinutes As Integer,
                   overtimeStep As OvertimeRounding, isActive As Boolean)
        Me.ShiftId = shiftId
        Me.Name = If(name, String.Empty)
        Me.StartTime = startTime
        Me.EndTime = endTime
        Me.UnpaidBreakMinutes = unpaidBreakMinutes
        Me.BreakAppliesAfterMinutes = breakAppliesAfterMinutes
        Me.FullDayMinutes = fullDayMinutes
        Me.HalfDayMinutes = halfDayMinutes
        Me.OvertimeStep = overtimeStep
        Me.IsActive = isActive
    End Sub

    Public ReadOnly Property ShiftId As Integer?
    Public ReadOnly Property Name As String
    Public ReadOnly Property StartTime As TimeSpan
    Public ReadOnly Property EndTime As TimeSpan
    Public ReadOnly Property UnpaidBreakMinutes As Integer
    Public ReadOnly Property BreakAppliesAfterMinutes As Integer
    Public ReadOnly Property FullDayMinutes As Integer
    Public ReadOnly Property HalfDayMinutes As Integer
    Public ReadOnly Property OvertimeStep As OvertimeRounding
    Public ReadOnly Property IsActive As Boolean

    Public Function ToDefinition() As ShiftDefinition
        Return New ShiftDefinition(Name, StartTime, EndTime, UnpaidBreakMinutes, BreakAppliesAfterMinutes, FullDayMinutes, HalfDayMinutes, OvertimeStep)
    End Function
End Class
