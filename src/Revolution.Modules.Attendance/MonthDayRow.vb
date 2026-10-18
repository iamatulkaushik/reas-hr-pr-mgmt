''' <summary>One employee's marked day inside a month. Day is Nothing for an employee with no marked day.</summary>
Public NotInheritable Class MonthDayRow
    Public Sub New(employeeId As Integer, empCode As String, employeeName As String, day As DayRecord)
        Me.EmployeeId = employeeId
        Me.EmpCode = If(empCode, String.Empty)
        Me.EmployeeName = If(employeeName, String.Empty)
        Me.Day = day
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property EmployeeName As String
    Public ReadOnly Property Day As DayRecord
End Class
