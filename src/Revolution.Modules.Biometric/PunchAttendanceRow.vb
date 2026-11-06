Imports Revolution.Modules.Attendance

''' <summary>One employee-day found from punches and what would be done with it. Status, WorkedMinutes and OvertimeMinutes are those the attendance engine judged; they are 0 when there is no judgement.</summary>
Public NotInheritable Class PunchAttendanceRow
    Public Sub New(employeeId As Integer, workDate As Date, shiftId As Integer?, shiftName As String, punchIn As Date?, punchOut As Date?, autoExit As Boolean,
                   status As AttendanceStatus?, workedMinutes As Integer, overtimeMinutes As Integer, action As PunchAttendanceAction, note As String)
        Me.EmployeeId = employeeId
        Me.WorkDate = workDate
        Me.ShiftId = shiftId
        Me.ShiftName = shiftName
        Me.PunchIn = punchIn
        Me.PunchOut = punchOut
        Me.AutoExit = autoExit
        Me.Status = status
        Me.WorkedMinutes = workedMinutes
        Me.OvertimeMinutes = overtimeMinutes
        Me.Action = action
        Me.Note = note
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property WorkDate As Date
    Public ReadOnly Property ShiftId As Integer?
    Public ReadOnly Property ShiftName As String
    Public ReadOnly Property PunchIn As Date?
    Public ReadOnly Property PunchOut As Date?
    ''' <summary>True when the exit was marked at the end of the assigned shift because there was only one punch.</summary>
    Public ReadOnly Property AutoExit As Boolean
    Public ReadOnly Property Status As AttendanceStatus?
    Public ReadOnly Property WorkedMinutes As Integer
    Public ReadOnly Property OvertimeMinutes As Integer
    Public ReadOnly Property Action As PunchAttendanceAction
    Public ReadOnly Property Note As String
End Class
