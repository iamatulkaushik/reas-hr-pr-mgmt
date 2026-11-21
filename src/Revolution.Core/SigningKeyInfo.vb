''' <summary>A newly made signing key: what is safe to share, and where the private half was saved.</summary>
Public NotInheritable Class SigningKeyInfo
    Public Sub New(keyId As String, publicKeyText As String, filePath As String)
        Me.KeyId = keyId
        Me.PublicKeyText = publicKeyText
        Me.FilePath = filePath
    End Sub

    Public ReadOnly Property KeyId As String
    ''' <summary>Base64 of the 64 bytes X then Y of the public key. Safe to share.</summary>
    Public ReadOnly Property PublicKeyText As String
    Public ReadOnly Property FilePath As String

    ''' <summary>The entry to add to LicenceKeys.Trusted in Revolution.Core, then ship a new build.</summary>
    Public ReadOnly Property TrustedLine As String
        Get
            Return $"{{""{KeyId}"", ""{PublicKeyText}""}}"
        End Get
    End Property
End Class
