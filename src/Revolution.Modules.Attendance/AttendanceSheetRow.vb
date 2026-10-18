''' <summary>One employee on one day. Status is Nothing while the day is not marked.</summary>
Public NotInheritable Class AttendanceSheetRow
    Public Sub New(employeeId As Integer, empCode As String, employeeName As String, status As AttendanceStatus?,
                   shiftId As Integer?, workedMinutes As Integer, overtimeMinutes As Integer)
        Me.EmployeeId = employeeId
        Me.EmpCode = If(empCode, String.Empty)
        Me.EmployeeName = If(employeeName, String.Empty)
        Me.Status = status
        Me.ShiftId = shiftId
        Me.WorkedMinutes = workedMinutes
        Me.OvertimeMinutes = overtimeMinutes
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property EmployeeName As String
    Public ReadOnly Property Status As AttendanceStatus?
    Public ReadOnly Property ShiftId As Integer?
    Public ReadOnly Property WorkedMinutes As Integer
    Public ReadOnly Property OvertimeMinutes As Integer
End Class
