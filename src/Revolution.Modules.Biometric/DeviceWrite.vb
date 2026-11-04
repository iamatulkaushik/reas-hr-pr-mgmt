''' <summary>A device to add (DeviceId Nothing) or change.</summary>
Public NotInheritable Class DeviceWrite
    Public Property DeviceId As Integer?
    Public Property FactoryId As Integer
    Public Property Code As String
    Public Property Name As String
    Public Property Brand As String
    Public Property Host As String
    Public Property Port As Integer
    Public Property IsActive As Boolean = True
End Class
