Imports System.Text

''' <summary>Makes a member name safe for the ECR: capitals, letters and single spaces only.</summary>
Public NotInheritable Class EcrNameCleaner
    Private Sub New()
    End Sub

    Public Shared Function Clean(name As String) As String
        If name Is Nothing Then
            Return String.Empty
        End If
        Dim builder As New StringBuilder(name.Length)
        For Each ch As Char In name.ToUpperInvariant()
            If ch >= "A"c AndAlso ch <= "Z"c Then
                builder.Append(ch)
            ElseIf Char.IsWhiteSpace(ch) OrElse ch = "."c Then
                builder.Append(" "c)
            End If
        Next
        Return String.Join(" ", builder.ToString().Split({" "c}, StringSplitOptions.RemoveEmptyEntries))
    End Function
End Class
