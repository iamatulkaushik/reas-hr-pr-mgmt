''' <summary>One marked day, ready for the month summary.</summary>
Public NotInheritable Class DayRecord
    Public Sub New(day As Date, status As AttendanceStatus, overtimeMinutes As Integer)
        If Not [Enum].IsDefined(GetType(AttendanceStatus), status) Then
            Throw New ArgumentOutOfRangeException(NameOf(status), "Unknown attendance status.")
        End If
        If overtimeMinutes < 0 Then
            Throw New ArgumentOutOfRangeException(NameOf(overtimeMinutes), "Overtime cannot be negative.")
        End If
        Me.Day = day.Date
        Me.Status = status
        Me.OvertimeMinutes = overtimeMinutes
    End Sub

    Public ReadOnly Property Day As Date
    Public ReadOnly Property Status As AttendanceStatus
    Public ReadOnly Property OvertimeMinutes As Integer
End Class
