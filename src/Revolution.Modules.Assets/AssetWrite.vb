''' <summary>A new asset: one tagged item, in stock when added.</summary>
Public NotInheritable Class AssetWrite
    Public Property AssetTypeId As Integer
    Public Property TagNo As String
    Public Property Description As String
    Public Property SerialNo As String
    Public Property PurchaseDate As Date
    Public Property PurchaseValue As Decimal
End Class
