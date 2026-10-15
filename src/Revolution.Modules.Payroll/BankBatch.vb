''' <summary>One salary payment run: a single debit and many credits.</summary>
Public NotInheritable Class BankBatch
    Public Sub New(debitAccount As String, debitBranchCode As String, paymentDate As Date,
                   batchReference As String, description As String, payees As IReadOnlyList(Of BankPayee))
        Me.DebitAccount = debitAccount
        Me.DebitBranchCode = debitBranchCode
        Me.PaymentDate = paymentDate
        Me.BatchReference = batchReference
        Me.Description = description
        Me.Payees = payees
    End Sub

    Public ReadOnly Property DebitAccount As String
    Public ReadOnly Property DebitBranchCode As String
    Public ReadOnly Property PaymentDate As Date
    Public ReadOnly Property BatchReference As String
    Public ReadOnly Property Description As String
    Public ReadOnly Property Payees As IReadOnlyList(Of BankPayee)
End Class
