''' <summary>Outcome of a service call. Services return this instead of showing message boxes.</summary>
Public NotInheritable Class OperationResult
    Private Sub New(succeeded As Boolean, message As String)
        Me.Succeeded = succeeded
        Me.Message = message
    End Sub

    Public ReadOnly Property Succeeded As Boolean
    Public ReadOnly Property Message As String

    Public Shared Function Ok() As OperationResult
        Return New OperationResult(True, String.Empty)
    End Function

    Public Shared Function Fail(message As String) As OperationResult
        If String.IsNullOrWhiteSpace(message) Then
            Throw New ArgumentException("A failure needs a message.", NameOf(message))
        End If
        Return New OperationResult(False, message)
    End Function
End Class
