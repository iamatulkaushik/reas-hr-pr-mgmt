''' <summary>One employee as loaded for the edit form. No protected ids. RowVersion goes back with an update to catch edits by someone else.</summary>
Public NotInheritable Class EmployeeDetail
    Public Property EmployeeId As Integer
    Public Property EmpCode As String = String.Empty
    Public Property FullName As String = String.Empty
    Public Property FatherOrSpouseName As String = String.Empty
    Public Property Gender As String = String.Empty
    Public Property DateOfBirth As Date
    Public Property JoinDate As Date
    Public Property ExitDate As Date?
    Public Property EmploymentTypeId As Integer
    Public Property WorkerTypeId As Integer
    Public Property PayType As PayType
    Public Property FactoryId As Integer
    Public Property FactoryName As String = String.Empty
    Public Property DepartmentId As Integer?
    Public Property DepartmentName As String = String.Empty
    Public Property SectionId As Integer?
    Public Property DesignationId As Integer?
    Public Property DesignationName As String = String.Empty
    Public Property GradeId As Integer?
    Public Property ReportingToEmployeeId As Integer?
    Public Property BiometricId As Integer?
    Public Property MaritalStatusId As Integer?
    Public Property Nationality As String = String.Empty
    Public Property Mobile As String = String.Empty
    Public Property Email As String = String.Empty
    Public Property Status As EmployeeStatus
    Public Property IsActive As Boolean
    Public Property IsAdolescent As Boolean
    Public Property RowVersion As Byte() = Array.Empty(Of Byte)()
End Class
