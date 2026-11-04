''' <summary>A registered device under a company and a location. LastPunchTime is the newest punch stored from it, in the device's own clock.</summary>
Public NotInheritable Class DeviceItem
    Public Sub New(deviceId As Integer, factoryId As Integer, factoryName As String, code As String, name As String, brand As String, host As String, port As Integer,
                   isActive As Boolean, lastPunchTime As Date?, lastPulledUtc As Date?)
        Me.DeviceId = deviceId
        Me.FactoryId = factoryId
        Me.FactoryName = factoryName
        Me.Code = code
        Me.Name = name
        Me.Brand = brand
        Me.Host = host
        Me.Port = port
        Me.IsActive = isActive
        Me.LastPunchTime = lastPunchTime
        Me.LastPulledUtc = lastPulledUtc
    End Sub

    Public ReadOnly Property DeviceId As Integer
    Public ReadOnly Property FactoryId As Integer
    Public ReadOnly Property FactoryName As String
    Public ReadOnly Property Code As String
    Public ReadOnly Property Name As String
    Public ReadOnly Property Brand As String
    Public ReadOnly Property Host As String
    Public ReadOnly Property Port As Integer
    Public ReadOnly Property IsActive As Boolean
    Public ReadOnly Property LastPunchTime As Date?
    Public ReadOnly Property LastPulledUtc As Date?
End Class
