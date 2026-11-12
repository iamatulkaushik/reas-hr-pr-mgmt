''' <summary>Take an asset back. ReturnStatus is InStock (fit for use) or Damaged.</summary>
Public NotInheritable Class AssetReturnWrite
    Public Property AssetId As Integer
    Public Property ReturnDate As Date
    Public Property Condition As String
    Public Property ReturnStatus As AssetStatus = AssetStatus.InStock
End Class
