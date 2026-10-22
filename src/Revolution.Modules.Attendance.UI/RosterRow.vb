''' <summary>One line of a roster list: an id the form uses to find the item again, and the cell texts in column order.</summary>
Friend NotInheritable Class RosterRow
    Public Sub New(id As Integer, cells As String())
        Me.Id = id
        Me.Cells = cells
    End Sub

    Public ReadOnly Property Id As Integer
    Public ReadOnly Property Cells As String()
End Class
