''' <summary>Weekly off days from a date. DaysMask uses bit 0 for Monday up to bit 6 for Sunday (migration 0204).</summary>
Public NotInheritable Class WeeklyOffRule
    Public Const MaximumMask As Integer = 127
    Private Const DaysInWeek As Integer = 7

    Public Sub New(effectiveFrom As Date, daysMask As Integer)
        Me.EffectiveFrom = effectiveFrom.Date
        Me.DaysMask = daysMask
    End Sub

    Public ReadOnly Property EffectiveFrom As Date
    Public ReadOnly Property DaysMask As Integer

    Public Function IsOff(day As DayOfWeek) As Boolean
        Dim bit As Integer = (CInt(day) + DaysInWeek - 1) Mod DaysInWeek
        Return (DaysMask And (1 << bit)) <> 0
    End Function
End Class
