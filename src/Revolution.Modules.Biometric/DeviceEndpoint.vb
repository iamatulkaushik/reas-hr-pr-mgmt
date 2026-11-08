''' <summary>Where and how to reach a device, for the brand reader: no company, no names. Built from a DeviceItem, or read by the agent from the server. The agent knows devices by code only, so DeviceId is 0 there.</summary>
Public NotInheritable Class DeviceEndpoint
    Public Sub New(deviceId As Integer, code As String, brand As String, host As String, port As Integer, Optional lastPunchTime As Date? = Nothing)
        Me.DeviceId = deviceId
        Me.Code = code
        Me.Brand = brand
        Me.Host = host
        Me.Port = port
        Me.LastPunchTime = lastPunchTime
    End Sub

    Public ReadOnly Property DeviceId As Integer
    Public ReadOnly Property Code As String
    Public ReadOnly Property Brand As String
    Public ReadOnly Property Host As String
    Public ReadOnly Property Port As Integer
    ''' <summary>The newest punch the server holds from this device, in the device's clock. Nothing when none yet. The next pull starts a few minutes before it.</summary>
    Public ReadOnly Property LastPunchTime As Date?

    Public Shared Function From(device As DeviceItem) As DeviceEndpoint
        If device Is Nothing Then Throw New ArgumentNullException(NameOf(device))
        Return New DeviceEndpoint(device.DeviceId, device.Code, device.Brand, device.Host, device.Port, device.LastPunchTime)
    End Function
End Class
