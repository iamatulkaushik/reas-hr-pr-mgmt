''' <summary>Storage for asset types, assets and hand-overs (ADR 0001). A rule the database enforces comes back as AssetsRuleException.</summary>
Public Interface IAssetRepository
    Function ListTypes(includeInactive As Boolean) As IReadOnlyList(Of AssetTypeItem)

    ''' <summary>Adds or changes an asset type. Returns its id.</summary>
    Function SaveType(assetType As AssetTypeWrite, userId As Integer?) As Integer

    ''' <summary>Assets, optionally of one status or held by one employee. HeldOnly lists only items that are out with an employee.</summary>
    Function ListAssets(status As AssetStatus?, employeeId As Integer?, heldOnly As Boolean) As IReadOnlyList(Of AssetItem)

    ''' <summary>Adds an asset, in stock. Returns its id.</summary>
    Function AddAsset(asset As AssetWrite, userId As Integer?) As Integer

    Sub IssueAsset(issue As AssetIssueWrite, userId As Integer?)

    Sub ReturnAsset(back As AssetReturnWrite, userId As Integer?)

    ''' <summary>Marks an asset Lost, Damaged or WrittenOff.</summary>
    Sub SetStatus(assetId As Integer, status As AssetStatus, userId As Integer?)

    Function History(assetId As Integer) As IReadOnlyList(Of AssetIssueItem)
End Interface
