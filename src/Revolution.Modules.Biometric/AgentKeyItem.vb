''' <summary>One registered punch agent (one computer on a factory LAN) of the company. The key itself is never stored or shown again after it is issued.</summary>
Public NotInheritable Class AgentKeyItem
    Public Sub New(registrationId As Integer, label As String, isActive As Boolean, createdUtc As Date, retiredUtc As Date?, lastSeenUtc As Date?)
        Me.RegistrationId = registrationId
        Me.Label = label
        Me.IsActive = isActive
        Me.CreatedUtc = createdUtc
        Me.RetiredUtc = retiredUtc
        Me.LastSeenUtc = lastSeenUtc
    End Sub

    Public ReadOnly Property RegistrationId As Integer
    Public ReadOnly Property Label As String
    Public ReadOnly Property IsActive As Boolean
    Public ReadOnly Property CreatedUtc As Date
    Public ReadOnly Property RetiredUtc As Date?
    ''' <summary>When the agent last asked the server, Nothing if never.</summary>
    Public ReadOnly Property LastSeenUtc As Date?
End Class
