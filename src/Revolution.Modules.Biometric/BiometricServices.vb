Imports Revolution.Core

''' <summary>The services of the Biometric module that screens use.</summary>
Public NotInheritable Class BiometricServices
    Public Sub New(biometric As BiometricService, punchAttendance As PunchAttendanceService, Optional factories As IFactoryLookup = Nothing, Optional agentKeys As AgentKeyService = Nothing)
        If biometric Is Nothing Then Throw New ArgumentNullException(NameOf(biometric))
        If punchAttendance Is Nothing Then Throw New ArgumentNullException(NameOf(punchAttendance))
        Me.Biometric = biometric
        Me.PunchAttendance = punchAttendance
        Me.Factories = factories
        Me.AgentKeys = agentKeys
    End Sub

    Public ReadOnly Property Biometric As BiometricService
    ''' <summary>Marks attendance from punches: preview and apply.</summary>
    Public ReadOnly Property PunchAttendance As PunchAttendanceService
    ''' <summary>The company's active locations for the device screen's location picker (IFactoryLookup, change request 0024).</summary>
    Public ReadOnly Property Factories As IFactoryLookup
    ''' <summary>Issue, list and retire agent keys.</summary>
    Public ReadOnly Property AgentKeys As AgentKeyService
End Class
