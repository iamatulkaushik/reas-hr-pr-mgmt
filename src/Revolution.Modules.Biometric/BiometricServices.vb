''' <summary>The services of the Biometric module that screens use.</summary>
Public NotInheritable Class BiometricServices
    Public Sub New(biometric As BiometricService, punchAttendance As PunchAttendanceService)
        If biometric Is Nothing Then Throw New ArgumentNullException(NameOf(biometric))
        If punchAttendance Is Nothing Then Throw New ArgumentNullException(NameOf(punchAttendance))
        Me.Biometric = biometric
        Me.PunchAttendance = punchAttendance
    End Sub

    Public ReadOnly Property Biometric As BiometricService
    ''' <summary>Marks attendance from punches: preview and apply.</summary>
    Public ReadOnly Property PunchAttendance As PunchAttendanceService
End Class
