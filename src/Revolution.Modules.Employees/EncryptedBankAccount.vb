''' <summary>The stored account ciphertext and the key version that made it. Cipher is Nothing when no account is on file.</summary>
Public NotInheritable Class EncryptedBankAccount
    Public Sub New(keyVersion As Byte, cipher As Byte())
        Me.KeyVersion = keyVersion
        Me.Cipher = cipher
    End Sub

    Public ReadOnly Property KeyVersion As Byte
    Public ReadOnly Property Cipher As Byte()
End Class
