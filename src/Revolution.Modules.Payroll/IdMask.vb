''' <summary>Masks an id to its last four characters for display, for example XXXXXXXX6789. An id with no stored last four shows as empty.</summary>
Public NotInheritable Class IdMask
    Private Const MaskChar As Char = "X"c
    Private Const MaskLength As Integer = 8

    Private Sub New()
    End Sub

    Public Shared Function Last4(lastFour As String) As String
        If String.IsNullOrWhiteSpace(lastFour) Then Return String.Empty
        Return New String(MaskChar, MaskLength) & lastFour.Trim()
    End Function
End Class
