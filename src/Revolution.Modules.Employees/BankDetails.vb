''' <summary>Bank details as edited on the form. The account number is plain only until the service encrypts it.</summary>
Public NotInheritable Class BankDetails
    Public Property PaymentMode As PaymentMode = PaymentMode.BankTransfer
    Public Property BankId As Integer?
    Public Property Ifsc As String = String.Empty
    Public Property Account As IdEdit = IdEdit.Unchanged
End Class
