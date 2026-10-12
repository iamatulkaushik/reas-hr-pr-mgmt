''' <summary>Result of creating an employee. IdWarning is filled when the employee was saved but the ids were not.</summary>
Public NotInheritable Class NewEmployee
    Public Sub New(employeeId As Integer, empCode As String, Optional idWarning As String = "")
        Me.EmployeeId = employeeId
        Me.EmpCode = If(empCode, String.Empty)
        Me.IdWarning = If(idWarning, String.Empty)
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property IdWarning As String
End Class
