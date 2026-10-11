''' <summary>Warns when overtime is above a cap. Warnings only: the screen decides whether to block.</summary>
Public NotInheritable Class OvertimeLimitChecker
    Private Sub New()
    End Sub

    Public Shared Function Check(weekMinutes As Integer, quarterMinutes As Integer, limits As OvertimeLimits) As IReadOnlyList(Of String)
        If limits Is Nothing Then
            Throw New ArgumentNullException(NameOf(limits))
        End If
        Dim warnings As New List(Of String)
        If weekMinutes > limits.WeeklyMaximumMinutes Then
            warnings.Add("Weekly overtime is above the limit.")
        End If
        If quarterMinutes > limits.QuarterlyMaximumMinutes Then
            warnings.Add("Quarterly overtime is above the limit.")
        End If
        Return warnings
    End Function
End Class
