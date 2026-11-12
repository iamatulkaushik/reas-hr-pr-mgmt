''' <summary>Storage for asset recoveries (ADR 0001). A rule the database enforces comes back as AssetsRuleException.</summary>
Public Interface IAssetRecoveryRepository
    ''' <summary>The asset's value, life and latest hand-over. Fails with "Asset not found." for an unknown asset.</summary>
    Function Source(assetId As Integer) As AssetRecoverySource

    ''' <summary>Records an open recovery. Returns its id.</summary>
    Function Add(recovery As AssetRecoveryAdd, userId As Integer?) As Integer

    ''' <summary>Cancels an open recovery. A recovery that a run holds or the settlement took cannot be cancelled.</summary>
    Sub Cancel(recoveryId As Integer, reason As String, userId As Integer?)

    Function List(employeeId As Integer?, status As AssetRecoveryStatus?) As IReadOnlyList(Of AssetRecoveryItem)
End Interface
