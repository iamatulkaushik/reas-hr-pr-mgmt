''' <summary>The time of one matched punch of one employee, in the device's clock.</summary>
Public NotInheritable Class EmployeePunchTime
    Public Sub New(employeeId As Integer, punchTime As Date)
        Me.EmployeeId = employeeId
        Me.PunchTime = punchTime
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property PunchTime As Date
End Class
