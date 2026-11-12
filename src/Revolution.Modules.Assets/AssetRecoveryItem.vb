''' <summary>One recovery for lists and the report. AppliedRunId is the regular run holding it, once step 0601b wires that.</summary>
Public NotInheritable Class AssetRecoveryItem
    Public Sub New(recoveryId As Integer, assetId As Integer, tagNo As String, typeName As String, employeeId As Integer, empCode As String, employeeName As String,
                   basisDate As Date, depreciatedValue As Decimal, amount As Decimal, reason As String, status As AssetRecoveryStatus, appliedRunId As Integer?)
        Me.RecoveryId = recoveryId
        Me.AssetId = assetId
        Me.TagNo = tagNo
        Me.TypeName = typeName
        Me.EmployeeId = employeeId
        Me.EmpCode = empCode
        Me.EmployeeName = employeeName
        Me.BasisDate = basisDate
        Me.DepreciatedValue = depreciatedValue
        Me.Amount = amount
        Me.Reason = reason
        Me.Status = status
        Me.AppliedRunId = appliedRunId
    End Sub

    Public ReadOnly Property RecoveryId As Integer
    Public ReadOnly Property AssetId As Integer
    Public ReadOnly Property TagNo As String
    Public ReadOnly Property TypeName As String
    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property EmployeeName As String
    Public ReadOnly Property BasisDate As Date
    Public ReadOnly Property DepreciatedValue As Decimal
    Public ReadOnly Property Amount As Decimal
    Public ReadOnly Property Reason As String
    Public ReadOnly Property Status As AssetRecoveryStatus
    Public ReadOnly Property AppliedRunId As Integer?
End Class
