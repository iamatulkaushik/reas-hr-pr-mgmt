Imports System.Security.Cryptography
Imports System.Text
Imports Revolution.Services

''' <summary>
''' The Developer's licence-signing key pair (ECDSA P-256, ADR 0018). The private half is kept only in a password-protected file
''' (PBKDF2-SHA256 and AES-256-GCM, the same container as the field key backup). It never goes in the repository or the app.
''' Only the public text (base64 of the 64 bytes X then Y) is shared, and goes into LicenceKeys.Trusted.
''' </summary>
Public NotInheritable Class SigningKey
    Private Const Header As String = "RVSK1"
    Private Const CoordinateLength As Integer = 32
    Private ReadOnly _parameters As ECParameters

    Private Sub New(keyId As String, parameters As ECParameters)
        Me.KeyId = keyId
        _parameters = parameters
    End Sub

    Public ReadOnly Property KeyId As String

    Public ReadOnly Property PublicKeyText As String
        Get
            Return Convert.ToBase64String(_parameters.Q.X.Concat(_parameters.Q.Y).ToArray())
        End Get
    End Property

    Public Shared Function Generate(keyId As String) As SigningKey
        If String.IsNullOrWhiteSpace(keyId) OrElse keyId.Contains(vbLf) Then
            Throw New ArgumentException("A key id is required, on one line.", NameOf(keyId))
        End If
        Using key = ECDsa.Create(ECCurve.NamedCurves.nistP256)
            Return New SigningKey(keyId.Trim(), key.ExportParameters(True))
        End Using
    End Function

    ''' <summary>Writes the protected key file. Throws ArgumentException when the password is shorter than 12 characters.</summary>
    Public Sub Save(filePath As String, password As String)
        Dim raw = _parameters.D.Concat(_parameters.Q.X).Concat(_parameters.Q.Y).ToArray()
        Dim text = Header & vbLf & KeyId & vbLf & Convert.ToBase64String(raw)
        Dim protectedBytes = FieldKeyBackup.Protect(Encoding.UTF8.GetBytes(text), password)
        IO.File.WriteAllText(filePath, Convert.ToBase64String(protectedBytes))
    End Sub

    ''' <summary>Throws CryptographicException for a wrong password or a file that is not a signing key.</summary>
    Public Shared Function Load(filePath As String, password As String) As SigningKey
        Dim data = Convert.FromBase64String(IO.File.ReadAllText(filePath).Trim())
        Dim text = Encoding.UTF8.GetString(FieldKeyBackup.Unprotect(data, password))
        Dim lines = text.Split(ControlChars.Lf)
        If lines.Length <> 3 OrElse lines(0) <> Header Then
            Throw New CryptographicException("This is not a licence signing key file.")
        End If
        Dim raw = Convert.FromBase64String(lines(2))
        If raw.Length <> CoordinateLength * 3 Then
            Throw New CryptographicException("This is not a licence signing key file.")
        End If
        Dim parameters As New ECParameters() With {.Curve = ECCurve.NamedCurves.nistP256}
        parameters.D = raw.Take(CoordinateLength).ToArray()
        parameters.Q.X = raw.Skip(CoordinateLength).Take(CoordinateLength).ToArray()
        parameters.Q.Y = raw.Skip(CoordinateLength * 2).Take(CoordinateLength).ToArray()
        Return New SigningKey(lines(1), parameters)
    End Function

    Public Function Sign(payload As Byte()) As Byte()
        Using key = ECDsa.Create(_parameters)
            Return key.SignData(payload, HashAlgorithmName.SHA256)
        End Using
    End Function
End Class
