''' <summary>What marking attendance from punches did: days marked, days refused (with the reason, by employee id and date) and days left alone.</summary>
Public NotInheritable Class PunchApplyResult
    Public Sub New(marked As Integer, skipped As Integer, failures As IReadOnlyList(Of String))
        Me.Marked = marked
        Me.Skipped = skipped
        Me.Failures = failures
    End Sub

    Public ReadOnly Property Marked As Integer
    Public ReadOnly Property Skipped As Integer
    Public ReadOnly Property Failures As IReadOnlyList(Of String)
End Class
