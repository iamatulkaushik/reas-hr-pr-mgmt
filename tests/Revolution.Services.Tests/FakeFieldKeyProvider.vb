Imports System.Security.Cryptography
Imports Revolution.Core

''' <summary>Test keys. Version 1 and 2 exist for encryption, version 1 for the index. Not for production.</summary>
Friend NotInheritable Class FakeFieldKeyProvider
    Implements IFieldKeyProvider

    Private ReadOnly _keys As New Dictionary(Of (FieldKeyPurpose, Byte), Byte())()

    Public Sub New(Optional current As Byte = 1)
        CurrentVersion = current
        _keys((FieldKeyPurpose.Encryption, CByte(1))) = RandomNumberGenerator.GetBytes(32)
        _keys((FieldKeyPurpose.Encryption, CByte(2))) = RandomNumberGenerator.GetBytes(32)
        _keys((FieldKeyPurpose.BlindIndex, CByte(1))) = RandomNumberGenerator.GetBytes(32)
    End Sub

    Public ReadOnly Property CurrentVersion As Byte Implements IFieldKeyProvider.CurrentVersion

    Public Sub SetKey(purpose As FieldKeyPurpose, version As Byte, key As Byte())
        _keys((purpose, version)) = key
    End Sub

    Public Function GetKey(purpose As FieldKeyPurpose, version As Byte) As Byte() Implements IFieldKeyProvider.GetKey
        Dim key As Byte() = Nothing
        If Not _keys.TryGetValue((purpose, version), key) Then
            Throw New InvalidOperationException("Key version not available.")
        End If
        Return key
    End Function
End Class
