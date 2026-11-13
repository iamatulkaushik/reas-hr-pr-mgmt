''' <summary>One claim for lists and reports.</summary>
Public NotInheritable Class ExpenseClaimItem
    Public Sub New(claimId As Integer, employeeId As Integer, empCode As String, employeeName As String, categoryId As Integer, categoryName As String,
                   expenseDate As Date, amount As Decimal, billNo As String, billDate As Date?, vendor As String, note As String,
                   status As ExpenseClaimStatus, decisionReason As String, paidDate As Date?, paymentMode As ExpensePaymentMode?, paymentRef As String)
        Me.ClaimId = claimId
        Me.EmployeeId = employeeId
        Me.EmpCode = empCode
        Me.EmployeeName = employeeName
        Me.CategoryId = categoryId
        Me.CategoryName = categoryName
        Me.ExpenseDate = expenseDate
        Me.Amount = amount
        Me.BillNo = billNo
        Me.BillDate = billDate
        Me.Vendor = vendor
        Me.Note = note
        Me.Status = status
        Me.DecisionReason = decisionReason
        Me.PaidDate = paidDate
        Me.PaymentMode = paymentMode
        Me.PaymentRef = paymentRef
    End Sub

    Public ReadOnly Property ClaimId As Integer
    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property EmployeeName As String
    Public ReadOnly Property CategoryId As Integer
    Public ReadOnly Property CategoryName As String
    Public ReadOnly Property ExpenseDate As Date
    Public ReadOnly Property Amount As Decimal
    Public ReadOnly Property BillNo As String
    Public ReadOnly Property BillDate As Date?
    Public ReadOnly Property Vendor As String
    Public ReadOnly Property Note As String
    Public ReadOnly Property Status As ExpenseClaimStatus
    Public ReadOnly Property DecisionReason As String
    Public ReadOnly Property PaidDate As Date?
    Public ReadOnly Property PaymentMode As ExpensePaymentMode?
    Public ReadOnly Property PaymentRef As String
End Class
