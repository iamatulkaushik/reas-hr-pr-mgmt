''' <summary>
''' One row of an import or a grid, with its values by template column name (not case sensitive; a missing column reads as empty).
''' RowNumber is the number the user sees in the list.
''' </summary>
Public NotInheritable Class ImportRow
    Public Sub New(rowNumber As Integer, values As IReadOnlyDictionary(Of String, String))
        If values Is Nothing Then Throw New ArgumentNullException(NameOf(values))
        Me.RowNumber = rowNumber
        Me.Values = values
    End Sub

    Public ReadOnly Property RowNumber As Integer
    Public ReadOnly Property Values As IReadOnlyDictionary(Of String, String)

    ''' <summary>The text of a column, trimmed; empty when the row has no such column or the cell is blank.</summary>
    Public Function [Get](column As String) As String
        Dim text As String = Nothing
        If Values.TryGetValue(column, text) AndAlso text IsNot Nothing Then
            Return text.Trim()
        End If
        Return String.Empty
    End Function
End Class
