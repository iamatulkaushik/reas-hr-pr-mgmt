''' <summary>One line of a master list: the row id, the cell texts in column order, and whether the row is active.</summary>
Friend NotInheritable Class MasterRow
    Public Sub New(id As Integer, cells As String(), isActive As Boolean)
        Me.Id = id
        Me.Cells = cells
        Me.IsActive = isActive
    End Sub

    Public ReadOnly Property Id As Integer
    Public ReadOnly Property Cells As String()
    Public ReadOnly Property IsActive As Boolean
End Class
