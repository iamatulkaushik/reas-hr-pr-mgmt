''' <summary>What the user did with one id on the form. Plain text is held only until the service encrypts it.</summary>
Public NotInheritable Class IdEdit
    Public Shared ReadOnly Unchanged As New IdEdit(IdAction.Unchanged, String.Empty)
    Public Shared ReadOnly Cleared As New IdEdit(IdAction.Clear, String.Empty)

    Private Sub New(action As IdAction, plain As String)
        Me.Action = action
        Me.Plain = plain
    End Sub

    Public ReadOnly Property Action As IdAction
    Public ReadOnly Property Plain As String

    Public Shared Function ReplaceWith(plain As String) As IdEdit
        Return New IdEdit(IdAction.Replace, If(plain, String.Empty))
    End Function

    ''' <summary>Blank text means nothing was typed, so the id stays as it is.</summary>
    Public Shared Function FromText(plain As String) As IdEdit
        Return If(String.IsNullOrWhiteSpace(plain), Unchanged, ReplaceWith(plain))
    End Function
End Class
