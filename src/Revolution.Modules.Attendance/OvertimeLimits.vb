''' <summary>Overtime caps in minutes. The caller supplies them from the statutory rate table. Never typed in code.</summary>
Public NotInheritable Class OvertimeLimits
    Public Sub New(weeklyMaximumMinutes As Integer, quarterlyMaximumMinutes As Integer)
        If weeklyMaximumMinutes < 1 Then
            Throw New ArgumentOutOfRangeException(NameOf(weeklyMaximumMinutes))
        End If
        If quarterlyMaximumMinutes < 1 Then
            Throw New ArgumentOutOfRangeException(NameOf(quarterlyMaximumMinutes))
        End If
        Me.WeeklyMaximumMinutes = weeklyMaximumMinutes
        Me.QuarterlyMaximumMinutes = quarterlyMaximumMinutes
    End Sub

    Public ReadOnly Property WeeklyMaximumMinutes As Integer
    Public ReadOnly Property QuarterlyMaximumMinutes As Integer
End Class
