''' <summary>One column of an import template: its name, whether it must be filled, a sentence for the instructions sheet, an example, and other names the same column may carry in a file.</summary>
Public NotInheritable Class ImportColumn
    Public Sub New(name As String, required As Boolean, description As String, Optional example As String = "", Optional aliases As IEnumerable(Of String) = Nothing)
        If String.IsNullOrWhiteSpace(name) Then Throw New ArgumentException("A column needs a name.", NameOf(name))
        Me.Name = name.Trim()
        Me.Required = required
        Me.Description = If(description, String.Empty)
        Me.Example = If(example, String.Empty)
        Me.Aliases = If(aliases Is Nothing, New List(Of String)(), New List(Of String)(aliases))
    End Sub

    Public ReadOnly Property Name As String
    Public ReadOnly Property Required As Boolean
    Public ReadOnly Property Description As String
    Public ReadOnly Property Example As String
    Public ReadOnly Property Aliases As IReadOnlyList(Of String)

    ''' <summary>The names a header may match, in the comparable form (letters and digits only, lower case).</summary>
    Friend Function Keys() As IReadOnlyList(Of String)
        Dim result As New List(Of String) From {ImportTemplate.Normalize(Name)}
        For Each otherName In Aliases
            Dim key As String = ImportTemplate.Normalize(otherName)
            If key.Length > 0 AndAlso Not result.Contains(key) Then
                result.Add(key)
            End If
        Next
        Return result
    End Function
End Class
