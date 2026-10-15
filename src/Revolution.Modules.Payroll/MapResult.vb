''' <summary>Mapped lines plus the employees left out, with the reason, so the screen can show them.</summary>
Public NotInheritable Class MapResult(Of T)
    Public Sub New(items As IReadOnlyList(Of T), problems As IReadOnlyList(Of String))
        Me.Items = items
        Me.Problems = problems
    End Sub

    Public ReadOnly Property Items As IReadOnlyList(Of T)
    Public ReadOnly Property Problems As IReadOnlyList(Of String)
End Class
