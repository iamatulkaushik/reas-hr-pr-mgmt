Imports System.Buffers.Binary
Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' Password-protected copy of the key set, for the backup the key store must offer (ADR 0011).
''' Layout: "RVKB1" + salt (16) + iterations (4) + nonce (12) + tag (16) + ciphertext. PBKDF2-SHA256, AES-256-GCM.
''' </summary>
Public NotInheritable Class FieldKeyBackup
    Private Const Magic As String = "RVKB1"
    Private Const SaltSize As Integer = 16
    Private Const NonceSize As Integer = 12
    Private Const TagSize As Integer = 16
    Public Const MinimumPasswordLength As Integer = 12
    Public Const Iterations As Integer = 210000

    Private Sub New()
    End Sub

    Public Shared Function Protect(payload As Byte(), password As String) As Byte()
        If payload Is Nothing OrElse payload.Length = 0 Then
            Throw New ArgumentException("There is nothing to back up.", NameOf(payload))
        End If
        RequireStrongPassword(password)
        Dim head = Encoding.ASCII.GetBytes(Magic)
        Dim salt = RandomNumberGenerator.GetBytes(SaltSize)
        Dim nonce = RandomNumberGenerator.GetBytes(NonceSize)
        Dim tag(TagSize - 1) As Byte
        Dim cipher(payload.Length - 1) As Byte
        Using aes As New AesGcm(Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32), TagSize)
            aes.Encrypt(nonce, payload, cipher, tag, head)
        End Using
        Dim result(head.Length + SaltSize + 4 + NonceSize + TagSize + cipher.Length - 1) As Byte
        Dim pos = 0
        Buffer.BlockCopy(head, 0, result, pos, head.Length) : pos += head.Length
        Buffer.BlockCopy(salt, 0, result, pos, SaltSize) : pos += SaltSize
        BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(pos, 4), Iterations) : pos += 4
        Buffer.BlockCopy(nonce, 0, result, pos, NonceSize) : pos += NonceSize
        Buffer.BlockCopy(tag, 0, result, pos, TagSize) : pos += TagSize
        Buffer.BlockCopy(cipher, 0, result, pos, cipher.Length)
        Return result
    End Function

    Public Shared Function Unprotect(data As Byte(), password As String) As Byte()
        Dim head = Encoding.ASCII.GetBytes(Magic)
        Dim fixedPart = head.Length + SaltSize + 4 + NonceSize + TagSize
        If data Is Nothing OrElse data.Length <= fixedPart OrElse Not data.AsSpan(0, head.Length).SequenceEqual(head) Then
            Throw New CryptographicException("The backup could not be opened.")
        End If
        Dim pos = head.Length
        Dim salt = data.AsSpan(pos, SaltSize).ToArray() : pos += SaltSize
        Dim rounds = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(pos, 4)) : pos += 4
        Dim nonce = data.AsSpan(pos, NonceSize).ToArray() : pos += NonceSize
        Dim tag = data.AsSpan(pos, TagSize).ToArray() : pos += TagSize
        Dim cipher = data.AsSpan(pos).ToArray()
        If rounds < 100000 OrElse rounds > 5000000 OrElse String.IsNullOrEmpty(password) Then
            Throw New CryptographicException("The backup could not be opened.")
        End If
        Dim plain(cipher.Length - 1) As Byte
        Try
            Using aes As New AesGcm(Rfc2898DeriveBytes.Pbkdf2(password, salt, rounds, HashAlgorithmName.SHA256, 32), TagSize)
                aes.Decrypt(nonce, cipher, tag, plain, head)
            End Using
        Catch ex As CryptographicException
            Throw New CryptographicException("The backup could not be opened.")
        End Try
        Return plain
    End Function

    Private Shared Sub RequireStrongPassword(password As String)
        If password Is Nothing OrElse password.Length < MinimumPasswordLength Then
            Throw New ArgumentException($"The backup password needs at least {MinimumPasswordLength} characters.", NameOf(password))
        End If
    End Sub
End Class
