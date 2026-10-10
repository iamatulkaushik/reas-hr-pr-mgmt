''' <summary>Shows only the last four characters of a sensitive id, grouped as XXXX-XXXX-1234.</summary>
Public NotInheritable Class SensitiveIdMask
    Private Const VisibleCount As Integer = 4
    Private Const GroupSize As Integer = 4

    Private Sub New()
    End Sub

    Public Shared Function Mask(text As String) As String
        Dim id As String = IdNormalizer.Compact(text)
        If id.Length = 0 Then
            Return String.Empty
        End If
        If id.Length <= VisibleCount Then
            Return New String("X"c, id.Length)
        End If
        Dim hidden As Integer = id.Length - VisibleCount
        Dim groups As New List(Of String)
        Dim remaining As Integer = hidden
        Do While remaining > 0
            Dim size As Integer = Math.Min(GroupSize, remaining)
            groups.Add(New String("X"c, size))
            remaining -= size
        Loop
        groups.Add(id.Substring(hidden))
        Return String.Join("-", groups)
    End Function
End Class
