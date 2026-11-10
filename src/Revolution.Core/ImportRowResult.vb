''' <summary>The result for one row. Message is the reason for a rejected row, or a short note for a saved one (for example the new employee code). It never repeats a cell.</summary>
Public NotInheritable Class ImportRowResult
    Public Sub New(rowNumber As Integer, outcome As ImportOutcome, message As String)
        Me.RowNumber = rowNumber
        Me.Outcome = outcome
        Me.Message = If(message, String.Empty)
    End Sub

    Public ReadOnly Property RowNumber As Integer
    Public ReadOnly Property Outcome As ImportOutcome
    Public ReadOnly Property Message As String
End Class
