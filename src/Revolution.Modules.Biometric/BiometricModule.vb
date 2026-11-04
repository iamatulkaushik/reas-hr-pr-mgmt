Imports Revolution.Data

''' <summary>Composition class of the Biometric module (ADR 0015). Revolution.App calls BiometricModule.Compose(services.Host) once. The agent does not use it: it has its own composition and its own login.</summary>
Public NotInheritable Class BiometricModule
    Private Sub New()
    End Sub

    Public Shared Function Compose(host As ModuleHost) As BiometricServices
        If host Is Nothing Then Throw New ArgumentNullException(NameOf(host))
        Return New BiometricServices(New BiometricService(New SqlBiometricRepository(host.Factory), host.Permissions, host.Entitlements, host.Audit, host.Session))
    End Function
End Class
