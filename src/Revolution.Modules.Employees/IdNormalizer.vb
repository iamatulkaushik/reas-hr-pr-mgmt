''' <summary>Strips spaces and hyphens so ids typed as 1234-5678-9012 or with spaces compare equal.</summary>
Friend NotInheritable Class IdNormalizer
    Private Sub New()
    End Sub

    Friend Shared Function Compact(text As String) As String
        If text Is Nothing Then
            Return String.Empty
        End If
        Return text.Replace(" ", String.Empty).Replace("-", String.Empty).Trim()
    End Function

    Friend Shared Function IsAllDigits(text As String) As Boolean
        If text.Length = 0 Then
            Return False
        End If
        For Each ch As Char In text
            If ch < "0"c OrElse ch > "9"c Then
                Return False
            End If
        Next
        Return True
    End Function
End Class
