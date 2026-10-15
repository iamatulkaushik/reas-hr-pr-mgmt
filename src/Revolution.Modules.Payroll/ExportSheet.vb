''' <summary>One sheet of an export workbook: a name and text cells. First row is the header. Move to Core with CR 0014.</summary>
Public NotInheritable Class ExportSheet
    Public Sub New(name As String, table As IReadOnlyList(Of String()))
        Me.Name = name
        Me.Table = table
    End Sub

    Public ReadOnly Property Name As String
    Public ReadOnly Property Table As IReadOnlyList(Of String())
End Class
