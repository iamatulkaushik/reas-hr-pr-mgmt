''' <summary>One asset type for lists and pickers.</summary>
Public NotInheritable Class AssetTypeItem
    Public Sub New(assetTypeId As Integer, name As String, usefulLifeMonths As Integer, isActive As Boolean)
        Me.AssetTypeId = assetTypeId
        Me.Name = name
        Me.UsefulLifeMonths = usefulLifeMonths
        Me.IsActive = isActive
    End Sub

    Public ReadOnly Property AssetTypeId As Integer
    Public ReadOnly Property Name As String
    ''' <summary>Months over which the item loses its value in a straight line (used for recovery at step 0601).</summary>
    Public ReadOnly Property UsefulLifeMonths As Integer
    Public ReadOnly Property IsActive As Boolean
End Class
