''' <summary>What a file reader returns: the header row and the data rows, every cell as text. Nothing here is interpreted yet (ADR 0025).</summary>
Public NotInheritable Class ImportTable
    Public Sub New(headers As IReadOnlyList(Of String), rows As IReadOnlyList(Of RawImportRow))
        If headers Is Nothing Then Throw New ArgumentNullException(NameOf(headers))
        If rows Is Nothing Then Throw New ArgumentNullException(NameOf(rows))
        Me.Headers = headers
        Me.Rows = rows
    End Sub

    Public ReadOnly Property Headers As IReadOnlyList(Of String)
    Public ReadOnly Property Rows As IReadOnlyList(Of RawImportRow)
End Class
