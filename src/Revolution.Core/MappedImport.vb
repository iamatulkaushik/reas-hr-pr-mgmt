''' <summary>A file after its headers were matched to a template: the rows by column name, and the file headers that matched no column (they are ignored).</summary>
Public NotInheritable Class MappedImport
    Public Sub New(rows As IReadOnlyList(Of ImportRow), ignoredColumns As IReadOnlyList(Of String))
        If rows Is Nothing Then Throw New ArgumentNullException(NameOf(rows))
        If ignoredColumns Is Nothing Then Throw New ArgumentNullException(NameOf(ignoredColumns))
        Me.Rows = rows
        Me.IgnoredColumns = ignoredColumns
    End Sub

    Public ReadOnly Property Rows As IReadOnlyList(Of ImportRow)
    Public ReadOnly Property IgnoredColumns As IReadOnlyList(Of String)
End Class
