''' <summary>One row of the employee list. No protected ids.</summary>
Public NotInheritable Class EmployeeSummary
    Public Property EmployeeId As Integer
    Public Property EmpCode As String = String.Empty
    Public Property FullName As String = String.Empty
    Public Property FactoryId As Integer
    Public Property FactoryName As String = String.Empty
    Public Property DepartmentName As String = String.Empty
    Public Property DesignationName As String = String.Empty
    Public Property JoinDate As Date
    Public Property Status As EmployeeStatus
    Public Property IsActive As Boolean
End Class
