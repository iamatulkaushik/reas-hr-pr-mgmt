''' <summary>What to do with one stored id: leave it, replace it, or clear it.</summary>
Public NotInheritable Class IdChange
    Public Shared ReadOnly Unchanged As New IdChange(IdChangeKind.Unchanged, Nothing)
    Public Shared ReadOnly Cleared As New IdChange(IdChangeKind.Cleared, Nothing)

    Private Sub New(kind As IdChangeKind, value As ProtectedId)
        Me.Kind = kind
        Me.Value = value
    End Sub

    Public ReadOnly Property Kind As IdChangeKind
    Public ReadOnly Property Value As ProtectedId

    Public Shared Function ReplacedWith(value As ProtectedId) As IdChange
        If value Is Nothing Then Throw New ArgumentNullException(NameOf(value))
        Return New IdChange(IdChangeKind.Replaced, value)
    End Function
End Class
