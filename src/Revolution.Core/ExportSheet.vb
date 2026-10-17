''' <summary>
''' One sheet of an export workbook (CR 0014): a name and a table of text cells. The first row is the header. Every cell is text, with no
''' formulas and no styling. The name follows the Excel 97-2003 rules: 1 to 31 characters and none of  [ ] : * ? / \
''' </summary>
Public NotInheritable Class ExportSheet
    Private Shared ReadOnly NotAllowed As Char() = {"["c, "]"c, ":"c, "*"c, "?"c, "/"c, "\"c}

    Public Sub New(name As String, table As IReadOnlyList(Of String()))
        If String.IsNullOrWhiteSpace(name) OrElse name.Length > 31 OrElse name.IndexOfAny(NotAllowed) >= 0 Then
            Throw New ArgumentException("A sheet name is 1 to 31 characters and cannot contain [ ] : * ? / \", NameOf(name))
        End If
        If table Is Nothing Then Throw New ArgumentNullException(NameOf(table))
        Me.Name = name
        Me.Table = table
    End Sub

    Public ReadOnly Property Name As String
    Public ReadOnly Property Table As IReadOnlyList(Of String())
End Class
