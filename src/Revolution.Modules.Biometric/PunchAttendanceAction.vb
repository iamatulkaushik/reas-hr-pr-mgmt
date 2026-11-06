''' <summary>What the attendance build does with one employee-day found from punches.</summary>
Public Enum PunchAttendanceAction
    ''' <summary>The day is not marked yet and can be marked from the punches.</summary>
    Mark = 1
    ''' <summary>The day already has a mark. Punches never overwrite one.</summary>
    AlreadyMarked = 2
    ''' <summary>The day is a weekly off. Enter it by hand, so work on an off day is a decision.</summary>
    WeeklyOff = 3
    Holiday = 4
    MonthClosed = 5
    ''' <summary>The employee has punches but no shift assigned for the day.</summary>
    NoShift = 6
    ''' <summary>The punches do not say enough: an unlikely span or a lone punch after the shift end.</summary>
    NeedsReview = 7
    ''' <summary>The shift has not ended yet, so an exit cannot be marked.</summary>
    ShiftNotEnded = 8
End Enum
