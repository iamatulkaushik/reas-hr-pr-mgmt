''' <summary>One punch as a device reported it, before anything is checked. DeviceUserId is the user id on the device (the employee's biometric id); CardNo is the RFID card number. One of the two is given.
''' PunchTime is the device's own clock, the factory's local time. VerifyMode and DeviceStatus are the device's own codes, kept as sent.</summary>
Public NotInheritable Class RawPunch
    Public Sub New(deviceUserId As Integer?, cardNo As String, punchTime As Date, Optional verifyMode As Byte? = Nothing, Optional deviceStatus As Byte? = Nothing)
        Me.DeviceUserId = deviceUserId
        Me.CardNo = cardNo
        Me.PunchTime = punchTime
        Me.VerifyMode = verifyMode
        Me.DeviceStatus = deviceStatus
    End Sub

    Public ReadOnly Property DeviceUserId As Integer?
    Public ReadOnly Property CardNo As String
    Public ReadOnly Property PunchTime As Date
    Public ReadOnly Property VerifyMode As Byte?
    Public ReadOnly Property DeviceStatus As Byte?
End Class
