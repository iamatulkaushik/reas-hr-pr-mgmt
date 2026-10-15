''' <summary>
''' SBI branch code is 5 digits. The company record keeps it directly (the IFSC field holds the branch code).
''' An employee record holds a full 11 character IFSC; its last 5 characters are the branch code.
''' Anything else is returned unchanged so the validator rejects it.
''' </summary>
Public NotInheritable Class SbiBranchCode
    Private Sub New()
    End Sub

    Public Shared Function Normalize(value As String) As String
        If value Is Nothing Then
            Return String.Empty
        End If
        Dim text As String = value.Trim().ToUpperInvariant()
        If text.Length = 11 Then
            Return text.Substring(6)
        End If
        Return text
    End Function
End Class
