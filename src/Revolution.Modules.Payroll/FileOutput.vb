''' <summary>A built file plus the employees left out and why, so the screen can show them before the file is saved.</summary>
Public NotInheritable Class FileOutput(Of T)
    Public Sub New(content As T, fileName As String, problems As IReadOnlyList(Of String))
        Me.Content = content
        Me.FileName = fileName
        Me.Problems = problems
    End Sub

    Public ReadOnly Property Content As T
    ''' <summary>Suggested file name. For bank files it is the base name; the screen adds the part suffix.</summary>
    Public ReadOnly Property FileName As String
    Public ReadOnly Property Problems As IReadOnlyList(Of String)
End Class
