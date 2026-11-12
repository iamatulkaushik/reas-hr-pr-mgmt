''' <summary>The services of the Assets module that screens use.</summary>
Public NotInheritable Class AssetsServices
    Public Sub New(assets As AssetService)
        If assets Is Nothing Then Throw New ArgumentNullException(NameOf(assets))
        Me.Assets = assets
    End Sub

    Public ReadOnly Property Assets As AssetService
End Class
