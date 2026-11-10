''' <summary>One data row as read from a file: the row number in the sheet (1 is the first row) and its cells as text.</summary>
Public NotInheritable Class RawImportRow
    Public Sub New(rowNumber As Integer, cells As String())
        If cells Is Nothing Then Throw New ArgumentNullException(NameOf(cells))
        Me.RowNumber = rowNumber
        Me.Cells = cells
    End Sub

    Public ReadOnly Property RowNumber As Integer
    Public ReadOnly Property Cells As String()
End Class
