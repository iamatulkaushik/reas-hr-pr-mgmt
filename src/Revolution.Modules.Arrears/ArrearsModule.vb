Imports Revolution.Data

''' <summary>Composition class of the Arrears module (ADR 0015). Revolution.App calls ArrearsModule.Compose(services.Host) once.</summary>
Public NotInheritable Class ArrearsModule
    Private Sub New()
    End Sub

    Public Shared Function Compose(host As ModuleHost) As ArrearsServices
        If host Is Nothing Then Throw New ArgumentNullException(NameOf(host))
        Return New ArrearsServices(New ArrearsService(New SqlArrearsRepository(host.Factory), host.Permissions, host.Entitlements, host.Audit, host.Session))
    End Function
End Class
