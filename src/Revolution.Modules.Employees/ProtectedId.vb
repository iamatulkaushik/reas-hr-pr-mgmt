''' <summary>One id ready for storage: ciphertext, blind index and the last four characters for masked display.</summary>
Public NotInheritable Class ProtectedId
    Public Sub New(cipher As Byte(), hash As Byte(), last4 As String)
        If cipher Is Nothing OrElse cipher.Length = 0 Then Throw New ArgumentException("Cipher is required.", NameOf(cipher))
        If hash Is Nothing OrElse hash.Length <> 32 Then Throw New ArgumentException("Hash must be 32 bytes.", NameOf(hash))
        If String.IsNullOrEmpty(last4) OrElse last4.Length > 4 Then Throw New ArgumentException("Last four characters are required.", NameOf(last4))
        Me.Cipher = cipher
        Me.Hash = hash
        Me.Last4 = last4
    End Sub

    ''' <summary>For a value that has no blind index column, such as a bank account number. Hash is Nothing.</summary>
    Friend Sub New(cipher As Byte(), last4 As String)
        If cipher Is Nothing OrElse cipher.Length = 0 Then Throw New ArgumentException("Cipher is required.", NameOf(cipher))
        If String.IsNullOrEmpty(last4) OrElse last4.Length > 4 Then Throw New ArgumentException("Last four characters are required.", NameOf(last4))
        Me.Cipher = cipher
        Me.Last4 = last4
    End Sub

    Public ReadOnly Property Cipher As Byte()
    Public ReadOnly Property Hash As Byte()
    Public ReadOnly Property Last4 As String
End Class
