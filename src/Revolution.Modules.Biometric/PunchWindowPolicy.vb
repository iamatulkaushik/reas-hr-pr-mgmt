''' <summary>
''' Which punches belong to a shift: from EarlyMinutes before it starts to LateMinutes after it ends (a night shift ends the next day). Punches closer together than MinGapMinutes are one punch
''' (a person who punches twice at the gate). The company's own values can replace the defaults later; the Owner has not asked for different ones.
''' </summary>
Public NotInheritable Class PunchWindowPolicy
    Public Const DefaultEarlyMinutes As Integer = 180
    Public Const DefaultLateMinutes As Integer = 360
    Public Const DefaultMinGapMinutes As Integer = 30

    Public Sub New(Optional earlyMinutes As Integer = DefaultEarlyMinutes, Optional lateMinutes As Integer = DefaultLateMinutes, Optional minGapMinutes As Integer = DefaultMinGapMinutes)
        If earlyMinutes < 0 OrElse earlyMinutes > 720 Then Throw New ArgumentOutOfRangeException(NameOf(earlyMinutes), "Early minutes must be from 0 to 720.")
        If lateMinutes < 0 OrElse lateMinutes > 720 Then Throw New ArgumentOutOfRangeException(NameOf(lateMinutes), "Late minutes must be from 0 to 720.")
        If minGapMinutes < 1 OrElse minGapMinutes > 240 Then Throw New ArgumentOutOfRangeException(NameOf(minGapMinutes), "The gap must be from 1 to 240 minutes.")
        Me.EarlyMinutes = earlyMinutes
        Me.LateMinutes = lateMinutes
        Me.MinGapMinutes = minGapMinutes
    End Sub

    Public ReadOnly Property EarlyMinutes As Integer
    Public ReadOnly Property LateMinutes As Integer
    Public ReadOnly Property MinGapMinutes As Integer
End Class
