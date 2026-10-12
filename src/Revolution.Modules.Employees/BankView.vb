''' <summary>Bank details for display. The account shows only its last four characters.</summary>
Public NotInheritable Class BankView
    Public Property PaymentMode As PaymentMode = PaymentMode.BankTransfer
    Public Property BankId As Integer?
    Public Property Ifsc As String = String.Empty
    Public Property AccountLast4 As String = String.Empty

    Public ReadOnly Property HasAccount As Boolean
        Get
            Return AccountLast4.Length > 0
        End Get
    End Property
End Class
