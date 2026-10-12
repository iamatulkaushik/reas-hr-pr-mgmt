''' <summary>Ciphertext of the four ids as stored, with the key version that made them. Nothing means no id of that kind.</summary>
Public NotInheritable Class EncryptedIdentifiers
    Public Sub New(keyVersion As Byte, uan As Byte(), esiIp As Byte(), pan As Byte(), aadhaar As Byte())
        Me.KeyVersion = keyVersion
        Me.Uan = uan
        Me.EsiIp = esiIp
        Me.Pan = pan
        Me.Aadhaar = aadhaar
    End Sub

    Public ReadOnly Property KeyVersion As Byte
    Public ReadOnly Property Uan As Byte()
    Public ReadOnly Property EsiIp As Byte()
    Public ReadOnly Property Pan As Byte()
    Public ReadOnly Property Aadhaar As Byte()

    Public ReadOnly Property HasAny As Boolean
        Get
            Return Uan IsNot Nothing OrElse EsiIp IsNot Nothing OrElse Pan IsNot Nothing OrElse Aadhaar IsNot Nothing
        End Get
    End Property
End Class
