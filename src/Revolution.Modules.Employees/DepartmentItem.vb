''' <summary>A department. It belongs to one factory and the factory cannot change after it is saved.</summary>
Public NotInheritable Class DepartmentItem
    Public Property DepartmentId As Integer
    Public Property FactoryId As Integer
    Public Property Code As String = String.Empty
    Public Property Name As String = String.Empty
    Public Property IsHazardous As Boolean
    Public Property IsActive As Boolean = True
End Class
