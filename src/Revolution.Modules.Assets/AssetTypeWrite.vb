''' <summary>An asset type to add (AssetTypeId Nothing) or change.</summary>
Public NotInheritable Class AssetTypeWrite
    Public Property AssetTypeId As Integer?
    Public Property Name As String
    Public Property UsefulLifeMonths As Integer
    Public Property IsActive As Boolean = True
End Class
