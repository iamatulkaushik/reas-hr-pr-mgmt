''' <summary>Bank details as they go to mst.usp_EmployeeBank_Save. Ifsc is compact upper case, or empty for none.</summary>
Public NotInheritable Class BankWrite
    Public Sub New(paymentMode As PaymentMode, bankId As Integer?, ifsc As String, keyVersion As Byte, account As IdChange)
        If account Is Nothing Then Throw New ArgumentNullException(NameOf(account))
        Me.PaymentMode = paymentMode
        Me.BankId = bankId
        Me.Ifsc = If(ifsc, String.Empty)
        Me.KeyVersion = keyVersion
        Me.Account = account
    End Sub

    Public ReadOnly Property PaymentMode As PaymentMode
    Public ReadOnly Property BankId As Integer?
    Public ReadOnly Property Ifsc As String
    Public ReadOnly Property KeyVersion As Byte
    Public ReadOnly Property Account As IdChange
End Class
