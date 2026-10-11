''' <summary>One shift and its day rules. End at or before start means the shift ends the next day.</summary>
Public NotInheritable Class ShiftDefinition
    Private Const MinutesPerDay As Integer = 1440

    Public Sub New(name As String, startTime As TimeSpan, endTime As TimeSpan, unpaidBreakMinutes As Integer,
                   breakAppliesAfterMinutes As Integer, fullDayMinutes As Integer, halfDayMinutes As Integer,
                   overtimeStep As OvertimeRounding)
        If String.IsNullOrWhiteSpace(name) Then
            Throw New ArgumentException("A shift needs a name.", NameOf(name))
        End If
        If startTime < TimeSpan.Zero OrElse startTime >= TimeSpan.FromDays(1) OrElse endTime < TimeSpan.Zero OrElse endTime >= TimeSpan.FromDays(1) Then
            Throw New ArgumentOutOfRangeException(NameOf(startTime), "Shift times must be within one day.")
        End If
        If startTime = endTime Then
            Throw New ArgumentException("Shift start and end cannot be equal.", NameOf(endTime))
        End If
        Me.Name = name.Trim()
        Me.StartTime = startTime
        Me.EndTime = endTime
        SpanMinutes = ComputeSpan(startTime, endTime)
        CheckRules(unpaidBreakMinutes, fullDayMinutes, halfDayMinutes)
        Me.UnpaidBreakMinutes = unpaidBreakMinutes
        Me.BreakAppliesAfterMinutes = breakAppliesAfterMinutes
        Me.FullDayMinutes = fullDayMinutes
        Me.HalfDayMinutes = halfDayMinutes
        Me.OvertimeStep = overtimeStep
    End Sub

    Public ReadOnly Property Name As String
    Public ReadOnly Property StartTime As TimeSpan
    Public ReadOnly Property EndTime As TimeSpan
    Public ReadOnly Property UnpaidBreakMinutes As Integer
    ''' <summary>The unpaid break is taken off only when the in-to-out span is longer than this.</summary>
    Public ReadOnly Property BreakAppliesAfterMinutes As Integer
    Public ReadOnly Property FullDayMinutes As Integer
    Public ReadOnly Property HalfDayMinutes As Integer
    Public ReadOnly Property OvertimeStep As OvertimeRounding
    ''' <summary>Start to end, break included.</summary>
    Public ReadOnly Property SpanMinutes As Integer

    ''' <summary>Paid working minutes in the shift. Overtime starts after this.</summary>
    Public ReadOnly Property ScheduledMinutes As Integer
        Get
            Return SpanMinutes - UnpaidBreakMinutes
        End Get
    End Property

    Private Sub CheckRules(unpaidBreakMinutes As Integer, fullDayMinutes As Integer, halfDayMinutes As Integer)
        If unpaidBreakMinutes < 0 OrElse unpaidBreakMinutes >= SpanMinutes Then
            Throw New ArgumentOutOfRangeException(NameOf(unpaidBreakMinutes), "Break must be shorter than the shift.")
        End If
        If halfDayMinutes < 1 OrElse halfDayMinutes >= fullDayMinutes Then
            Throw New ArgumentException("Half day minutes must be above zero and below full day minutes.", NameOf(halfDayMinutes))
        End If
        If fullDayMinutes > SpanMinutes - unpaidBreakMinutes Then
            Throw New ArgumentException("Full day minutes cannot exceed the paid shift minutes.", NameOf(fullDayMinutes))
        End If
    End Sub

    Private Shared Function ComputeSpan(startTime As TimeSpan, endTime As TimeSpan) As Integer
        Dim diff As TimeSpan = endTime - startTime
        If diff <= TimeSpan.Zero Then
            diff += TimeSpan.FromDays(1)
        End If
        Return Math.Min(CInt(Math.Floor(diff.TotalMinutes)), MinutesPerDay)
    End Function
End Class
