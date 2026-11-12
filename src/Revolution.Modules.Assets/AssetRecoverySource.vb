''' <summary>What the database knows about an asset for a recovery: its value, life and the employee of its latest hand-over (HolderEmployeeId is Nothing when it was never issued).</summary>
Public NotInheritable Class AssetRecoverySource
    Public Sub New(assetId As Integer, tagNo As String, typeName As String, purchaseDate As Date, purchaseValue As Decimal, usefulLifeMonths As Integer,
                   status As AssetStatus, issueId As Integer?, employeeId As Integer?, empCode As String, employeeName As String)
        Me.AssetId = assetId
        Me.TagNo = tagNo
        Me.TypeName = typeName
        Me.PurchaseDate = purchaseDate
        Me.PurchaseValue = purchaseValue
        Me.UsefulLifeMonths = usefulLifeMonths
        Me.Status = status
        Me.IssueId = issueId
        Me.EmployeeId = employeeId
        Me.EmpCode = empCode
        Me.EmployeeName = employeeName
    End Sub

    Public ReadOnly Property AssetId As Integer
    Public ReadOnly Property TagNo As String
    Public ReadOnly Property TypeName As String
    Public ReadOnly Property PurchaseDate As Date
    Public ReadOnly Property PurchaseValue As Decimal
    Public ReadOnly Property UsefulLifeMonths As Integer
    Public ReadOnly Property Status As AssetStatus
    Public ReadOnly Property IssueId As Integer?
    Public ReadOnly Property EmployeeId As Integer?
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property EmployeeName As String
End Class
