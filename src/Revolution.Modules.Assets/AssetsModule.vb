Imports Revolution.Data

''' <summary>Composition class of the Assets module (ADR 0015). Revolution.App calls AssetsModule.Compose(services.Host) once, when the screens are wired (Phase 9 step 0603).</summary>
Public NotInheritable Class AssetsModule
    Private Sub New()
    End Sub

    Public Shared Function Compose(host As ModuleHost) As AssetsServices
        If host Is Nothing Then Throw New ArgumentNullException(NameOf(host))
        Dim today As Func(Of Date) = Function() Date.Today
        Return New AssetsServices(
            New AssetService(New SqlAssetRepository(host.Factory), host.Permissions, host.Entitlements, host.Audit, host.Session, today),
            New AssetRecoveryService(New SqlAssetRecoveryRepository(host.Factory), host.Permissions, host.Entitlements, host.Audit, host.Session, today))
    End Function
End Class
