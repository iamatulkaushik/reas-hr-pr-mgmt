''' <summary>A designation. CategoryId: 1 Unskilled, 2 Semi-skilled, 3 Skilled, 4 Highly skilled, 5 Clerical, 6 Supervisory, 7 Managerial.</summary>
Public NotInheritable Class DesignationItem
    Public Property DesignationId As Integer
    Public Property Code As String = String.Empty
    Public Property Name As String = String.Empty
    Public Property GradeId As Integer?
    Public Property CategoryId As Integer
    Public Property IsActive As Boolean = True
End Class
