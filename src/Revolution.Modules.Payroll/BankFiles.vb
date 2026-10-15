''' <summary>The text files a bank needs for one batch.</summary>
Public NotInheritable Class BankFiles
    Public Sub New(beneficiaryText As String, transactionText As String)
        Me.BeneficiaryText = beneficiaryText
        Me.TransactionText = transactionText
    End Sub

    Public ReadOnly Property BeneficiaryText As String
    Public ReadOnly Property TransactionText As String
End Class
