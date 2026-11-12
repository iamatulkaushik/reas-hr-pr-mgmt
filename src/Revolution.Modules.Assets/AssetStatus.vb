''' <summary>Where an asset is. Values match ast.Asset.StatusID. A returned item goes back to InStock (or Damaged).</summary>
Public Enum AssetStatus
    InStock = 1
    Issued = 2
    Lost = 3
    Damaged = 4
    WrittenOff = 5
End Enum
