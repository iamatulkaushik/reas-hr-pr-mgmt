''' <summary>An id or card that devices reported and that matches no employee yet, with how often and when. Link it to an employee and the waiting punches are matched.</summary>
Public NotInheritable Class UnknownBiometricUser
    Public Sub New(deviceUserId As Integer?, cardNo As String, punchCount As Integer, firstSeen As Date, lastSeen As Date, deviceCount As Integer)
        Me.DeviceUserId = deviceUserId
        Me.CardNo = cardNo
        Me.PunchCount = punchCount
        Me.FirstSeen = firstSeen
        Me.LastSeen = lastSeen
        Me.DeviceCount = deviceCount
    End Sub

    Public ReadOnly Property DeviceUserId As Integer?
    Public ReadOnly Property CardNo As String
    Public ReadOnly Property PunchCount As Integer
    Public ReadOnly Property FirstSeen As Date
    Public ReadOnly Property LastSeen As Date
    Public ReadOnly Property DeviceCount As Integer
End Class
