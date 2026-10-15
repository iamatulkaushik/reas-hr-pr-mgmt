''' <summary>SBI delimiters. The format sheet shows | for both files; the sample lines show #. Confirm with the bank.</summary>
Public NotInheritable Class SbiFileOptions
    Public Sub New(Optional beneficiaryDelimiter As String = "|", Optional transactionDelimiter As String = "|")
        Me.BeneficiaryDelimiter = beneficiaryDelimiter
        Me.TransactionDelimiter = transactionDelimiter
    End Sub

    Public ReadOnly Property BeneficiaryDelimiter As String
    Public ReadOnly Property TransactionDelimiter As String
End Class
