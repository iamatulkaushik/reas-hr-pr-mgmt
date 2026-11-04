''' <summary>Where and how to reach a device, for the brand reader: no company, no names. Built from a DeviceItem.</summary>
Public NotInheritable Class DeviceEndpoint
    Public Sub New(deviceId As Integer, code As String, brand As String, host As String, port As Integer)
        Me.DeviceId = deviceId
        Me.Code = code
        Me.Brand = brand
        Me.Host = host
        Me.Port = port
    End Sub

    Public ReadOnly Property DeviceId As Integer
    Public ReadOnly Property Code As String
    Public ReadOnly Property Brand As String
    Public ReadOnly Property Host As String
    Public ReadOnly Property Port As Integer

    Public Shared Function From(device As DeviceItem) As DeviceEndpoint
        If device Is Nothing Then Throw New ArgumentNullException(NameOf(device))
        Return New DeviceEndpoint(device.DeviceId, device.Code, device.Brand, device.Host, device.Port)
    End Function
End Class
