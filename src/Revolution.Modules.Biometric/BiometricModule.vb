Imports Revolution.Data
Imports Revolution.Modules.Attendance

''' <summary>Composition class of the Biometric module (ADR 0015). Revolution.App calls BiometricModule.Compose(services.Host) once. The agent does not use it: it has its own composition and its own login.</summary>
Public NotInheritable Class BiometricModule
    Private Sub New()
    End Sub

    Public Shared Function Compose(host As ModuleHost) As BiometricServices
        If host Is Nothing Then Throw New ArgumentNullException(NameOf(host))
        Dim repository As New SqlBiometricRepository(host.Factory)
        Dim roster As New SqlRosterRepository(host.Factory)
        Dim attendance As New SqlAttendanceRepository(host.Factory)
        Dim marker As New AttendanceService(attendance, host.Permissions, host.Entitlements, host.Audit, host.Session, Function() Date.Today)
        Return New BiometricServices(New BiometricService(repository, host.Permissions, host.Entitlements, host.Audit, host.Session),
                                     New PunchAttendanceService(repository, roster, attendance, marker, host.Permissions, host.Entitlements, host.Audit, Function() Date.Now))
    End Function
End Class
