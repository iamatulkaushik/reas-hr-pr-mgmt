''' <summary>One marked day of one employee with the shift and worked minutes kept beside the status, so an edit does not lose them.</summary>
Public NotInheritable Class EmployeeDayRow
    Public Sub New(day As Date, status As AttendanceStatus, shiftId As Integer?, workedMinutes As Integer, overtimeMinutes As Integer)
        Me.Day = day.Date
        Me.Status = status
        Me.ShiftId = shiftId
        Me.WorkedMinutes = workedMinutes
        Me.OvertimeMinutes = overtimeMinutes
    End Sub

    Public ReadOnly Property Day As Date
    Public ReadOnly Property Status As AttendanceStatus
    Public ReadOnly Property ShiftId As Integer?
    Public ReadOnly Property WorkedMinutes As Integer
    Public ReadOnly Property OvertimeMinutes As Integer
End Class
