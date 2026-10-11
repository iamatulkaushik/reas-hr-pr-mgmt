Imports System.Security.Cryptography
Imports System.Text
Imports Revolution.Core

''' <summary>AES-256-GCM field encryption and HMAC-SHA256 blind index (ADR 0011). Keys come from IFieldKeyProvider.</summary>
Public NotInheritable Class AesGcmFieldCrypto
    Implements IFieldCrypto

    Private Const KeySize As Integer = 32
    Private Const NonceSize As Integer = 12
    Private Const TagSize As Integer = 16
    ''' <summary>The index key has its own version that does not move with the encryption key. Changing it means rebuilding every index value.</summary>
    Private Const BlindIndexKeyVersion As Byte = 1

    Private ReadOnly _keys As IFieldKeyProvider

    Public Sub New(keys As IFieldKeyProvider)
        If keys Is Nothing Then
            Throw New ArgumentNullException(NameOf(keys))
        End If
        _keys = keys
    End Sub

    Public ReadOnly Property CurrentKeyVersion As Byte Implements IFieldCrypto.CurrentKeyVersion
        Get
            Return _keys.CurrentVersion
        End Get
    End Property

    Public Function Encrypt(plain As String, context As String) As Byte() Implements IFieldCrypto.Encrypt
        If String.IsNullOrEmpty(plain) Then
            Throw New ArgumentException("There is nothing to encrypt.", NameOf(plain))
        End If
        RequireContext(context)
        Dim plainBytes = Encoding.UTF8.GetBytes(plain)
        Dim nonce = RandomNumberGenerator.GetBytes(NonceSize)
        Dim tag(TagSize - 1) As Byte
        Dim cipher(plainBytes.Length - 1) As Byte
        Using aes As New AesGcm(KeyFor(FieldKeyPurpose.Encryption, _keys.CurrentVersion), TagSize)
            aes.Encrypt(nonce, plainBytes, cipher, tag, Encoding.UTF8.GetBytes(context))
        End Using
        Dim result(NonceSize + TagSize + cipher.Length - 1) As Byte
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize)
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize)
        Buffer.BlockCopy(cipher, 0, result, NonceSize + TagSize, cipher.Length)
        Return result
    End Function

    Public Function Decrypt(cipher As Byte(), keyVersion As Byte, context As String) As String Implements IFieldCrypto.Decrypt
        If cipher Is Nothing OrElse cipher.Length <= NonceSize + TagSize Then
            Throw New CryptographicException("The value could not be decrypted.")
        End If
        RequireContext(context)
        Dim nonce(NonceSize - 1) As Byte
        Dim tag(TagSize - 1) As Byte
        Dim body(cipher.Length - NonceSize - TagSize - 1) As Byte
        Buffer.BlockCopy(cipher, 0, nonce, 0, NonceSize)
        Buffer.BlockCopy(cipher, NonceSize, tag, 0, TagSize)
        Buffer.BlockCopy(cipher, NonceSize + TagSize, body, 0, body.Length)
        Dim plain(body.Length - 1) As Byte
        Try
            Using aes As New AesGcm(KeyFor(FieldKeyPurpose.Encryption, keyVersion), TagSize)
                aes.Decrypt(nonce, body, tag, plain, Encoding.UTF8.GetBytes(context))
            End Using
        Catch ex As CryptographicException
            ' One plain message: the reason (wrong key, wrong context, changed data) must not help an attacker.
            Throw New CryptographicException("The value could not be decrypted.")
        End Try
        Return Encoding.UTF8.GetString(plain)
    End Function

    Public Function BlindIndex(companyId As Integer, field As String, plain As String) As Byte() Implements IFieldCrypto.BlindIndex
        If String.IsNullOrWhiteSpace(field) Then
            Throw New ArgumentException("A field name is required.", NameOf(field))
        End If
        If String.IsNullOrWhiteSpace(plain) Then
            Throw New ArgumentException("There is nothing to index.", NameOf(plain))
        End If
        Dim normal = plain.Replace(" ", String.Empty).Replace("-", String.Empty).Trim().ToUpperInvariant()
        Dim data = Encoding.UTF8.GetBytes(String.Concat(companyId.ToString(Globalization.CultureInfo.InvariantCulture), ":", field.ToUpperInvariant(), ":", normal))
        Return HMACSHA256.HashData(KeyFor(FieldKeyPurpose.BlindIndex, BlindIndexKeyVersion), data)
    End Function

    Private Function KeyFor(purpose As FieldKeyPurpose, version As Byte) As Byte()
        Dim key = _keys.GetKey(purpose, version)
        If key Is Nothing OrElse key.Length <> KeySize Then
            Throw New InvalidOperationException("A field key must be exactly 32 bytes.")
        End If
        Return key
    End Function

    Private Shared Sub RequireContext(context As String)
        If String.IsNullOrEmpty(context) Then
            Throw New ArgumentException("A context is required.", NameOf(context))
        End If
    End Sub
End Class
