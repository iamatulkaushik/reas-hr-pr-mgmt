''' <summary>The services of the Assets module that screens use.</summary>
Public NotInheritable Class AssetsServices
    Public Sub New(assets As AssetService, recoveries As AssetRecoveryService)
        If assets Is Nothing Then Throw New ArgumentNullException(NameOf(assets))
        If recoveries Is Nothing Then Throw New ArgumentNullException(NameOf(recoveries))
        Me.Assets = assets
        Me.Recoveries = recoveries
    End Sub

    Public ReadOnly Property Assets As AssetService
    Public ReadOnly Property Recoveries As AssetRecoveryService
End Class
