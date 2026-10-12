''' <summary>Where an employee works and how they are classed. Ids are database keys from the form dropdowns.</summary>
Public NotInheritable Class EmployeePlacement
    ''' <summary>M, F or O.</summary>
    Public Property Gender As String = String.Empty
    Public Property FatherOrSpouseName As String = String.Empty
    ''' <summary>1 Permanent, 2 Contract, 3 Fixed term, 4 Casual, 5 Apprentice, 6 Trainee.</summary>
    Public Property EmploymentTypeId As Integer
    ''' <summary>1 Worker, 2 Staff, 3 Manager.</summary>
    Public Property WorkerTypeId As Integer = 1
    Public Property FactoryId As Integer
    Public Property DepartmentId As Integer?
    Public Property SectionId As Integer?
    Public Property DesignationId As Integer?
    Public Property GradeId As Integer?
    Public Property ReportingToEmployeeId As Integer?
    Public Property BiometricId As Integer?
    ''' <summary>1 Single, 2 Married, 3 Widowed, 4 Divorced. Nothing when not given.</summary>
    Public Property MaritalStatusId As Integer?
    Public Property Nationality As String = "Indian"
    Public Property Email As String = String.Empty
End Class
