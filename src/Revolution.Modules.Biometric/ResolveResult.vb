''' <summary>What matching waiting punches to employees gave.</summary>
Public NotInheritable Class ResolveResult
    Public Sub New(resolved As Integer, stillUnmatched As Integer)
        Me.Resolved = resolved
        Me.StillUnmatched = stillUnmatched
    End Sub

    Public ReadOnly Property Resolved As Integer
    Public ReadOnly Property StillUnmatched As Integer
End Class
