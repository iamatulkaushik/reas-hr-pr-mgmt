''' <summary>Outcome of a service call that also returns a value on success.</summary>
Public NotInheritable Class OperationResult(Of T)
    Private Sub New(succeeded As Boolean, message As String, value As T)
        Me.Succeeded = succeeded
        Me.Message = message
        Me.Value = value
    End Sub

    Public ReadOnly Property Succeeded As Boolean
    Public ReadOnly Property Message As String
    ''' <summary>The result value. Only meaningful when Succeeded is True.</summary>
    Public ReadOnly Property Value As T

    Public Shared Function Ok(value As T) As OperationResult(Of T)
        Return New OperationResult(Of T)(True, String.Empty, value)
    End Function

    Public Shared Function Fail(message As String) As OperationResult(Of T)
        If String.IsNullOrWhiteSpace(message) Then
            Throw New ArgumentException("A failure needs a message.", NameOf(message))
        End If
        Return New OperationResult(Of T)(False, message, Nothing)
    End Function
End Class
