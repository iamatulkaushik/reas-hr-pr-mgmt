''' <summary>What the arrears list shows. Nothing means no limit on that field. Cancelled claims are hidden unless asked for.</summary>
Public NotInheritable Class ArrearsFilter
    Public Property EmployeeId As Integer?
    Public Property FromMonth As Date?
    Public Property ToMonth As Date?
    Public Property IncludeCancelled As Boolean
End Class
