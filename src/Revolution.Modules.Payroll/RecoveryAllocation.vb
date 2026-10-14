''' <summary>Part of an EMI taken against one installment.</summary>
Public NotInheritable Class RecoveryAllocation
    Public Sub New(loanId As Integer, installmentNo As Integer, amount As Decimal)
        Me.LoanID = loanId
        Me.InstallmentNo = installmentNo
        Me.Amount = amount
    End Sub

    Public ReadOnly Property LoanID As Integer
    Public ReadOnly Property InstallmentNo As Integer
    Public ReadOnly Property Amount As Decimal
End Class
