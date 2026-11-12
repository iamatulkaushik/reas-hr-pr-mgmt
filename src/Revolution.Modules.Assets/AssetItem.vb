''' <summary>One asset with its type and, while it is out, the employee who holds it.</summary>
Public NotInheritable Class AssetItem
    Public Sub New(assetId As Integer, assetTypeId As Integer, typeName As String, tagNo As String, description As String, serialNo As String,
                   purchaseDate As Date, purchaseValue As Decimal, status As AssetStatus,
                   holderEmployeeId As Integer?, holderCode As String, holderName As String, issueDate As Date?, expectedReturnDate As Date?)
        Me.AssetId = assetId
        Me.AssetTypeId = assetTypeId
        Me.TypeName = typeName
        Me.TagNo = tagNo
        Me.Description = description
        Me.SerialNo = serialNo
        Me.PurchaseDate = purchaseDate
        Me.PurchaseValue = purchaseValue
        Me.Status = status
        Me.HolderEmployeeId = holderEmployeeId
        Me.HolderCode = holderCode
        Me.HolderName = holderName
        Me.IssueDate = issueDate
        Me.ExpectedReturnDate = expectedReturnDate
    End Sub

    Public ReadOnly Property AssetId As Integer
    Public ReadOnly Property AssetTypeId As Integer
    Public ReadOnly Property TypeName As String
    Public ReadOnly Property TagNo As String
    Public ReadOnly Property Description As String
    Public ReadOnly Property SerialNo As String
    Public ReadOnly Property PurchaseDate As Date
    Public ReadOnly Property PurchaseValue As Decimal
    Public ReadOnly Property Status As AssetStatus
    ''' <summary>The employee holding the item now, or Nothing when it is not out.</summary>
    Public ReadOnly Property HolderEmployeeId As Integer?
    Public ReadOnly Property HolderCode As String
    Public ReadOnly Property HolderName As String
    Public ReadOnly Property IssueDate As Date?
    Public ReadOnly Property ExpectedReturnDate As Date?
End Class
