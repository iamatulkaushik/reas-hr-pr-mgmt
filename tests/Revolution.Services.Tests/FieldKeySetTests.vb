Imports System.Security.Cryptography
Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class FieldKeySetTests
    <Fact>
    Public Sub CreateNew_HasVersionOneKeys_Of32Bytes()
        Dim keys = FieldKeySet.CreateNew()
        Assert.Equal(CByte(1), keys.CurrentVersion)
        Assert.Equal(32, keys.GetKey(FieldKeyPurpose.Encryption, 1).Length)
        Assert.Equal(32, keys.GetKey(FieldKeyPurpose.BlindIndex, 1).Length)
    End Sub

    <Fact>
    Public Sub EncryptionKey_AndIndexKey_AreDifferent()
        Dim keys = FieldKeySet.CreateNew()
        Assert.False(keys.GetKey(FieldKeyPurpose.Encryption, 1).AsSpan().SequenceEqual(keys.GetKey(FieldKeyPurpose.BlindIndex, 1)))
    End Sub

    <Fact>
    Public Sub Serialize_RoundTrip_KeepsEveryKey()
        Dim keys = FieldKeySet.CreateNew()
        keys.AddEncryptionVersion()
        Dim copy = FieldKeySet.Deserialize(keys.Serialize())
        Assert.Equal(CByte(2), copy.CurrentVersion)
        Assert.True(copy.GetKey(FieldKeyPurpose.Encryption, 1).AsSpan().SequenceEqual(keys.GetKey(FieldKeyPurpose.Encryption, 1)))
        Assert.True(copy.GetKey(FieldKeyPurpose.Encryption, 2).AsSpan().SequenceEqual(keys.GetKey(FieldKeyPurpose.Encryption, 2)))
        Assert.True(copy.GetKey(FieldKeyPurpose.BlindIndex, 1).AsSpan().SequenceEqual(keys.GetKey(FieldKeyPurpose.BlindIndex, 1)))
    End Sub

    <Fact>
    Public Sub AddEncryptionVersion_KeepsOldKeys_AndTheIndexKey()
        Dim keys = FieldKeySet.CreateNew()
        Dim oldKey = keys.GetKey(FieldKeyPurpose.Encryption, 1).ToArray()
        Dim index = keys.GetKey(FieldKeyPurpose.BlindIndex, 1).ToArray()
        Assert.Equal(CByte(2), keys.AddEncryptionVersion())
        Assert.True(keys.GetKey(FieldKeyPurpose.Encryption, 1).AsSpan().SequenceEqual(oldKey))
        Assert.True(keys.GetKey(FieldKeyPurpose.BlindIndex, 1).AsSpan().SequenceEqual(index))
        Assert.False(keys.GetKey(FieldKeyPurpose.Encryption, 2).AsSpan().SequenceEqual(oldKey))
    End Sub

    <Fact>
    Public Sub OldRows_DecryptAfterRotation_ThroughTheRealCrypto()
        Dim keys = FieldKeySet.CreateNew()
        Dim ctx = FieldContext.Build(7, 42, FieldContext.Aadhaar)
        Dim before = New AesGcmFieldCrypto(keys).Encrypt("123456789012", ctx)
        keys.AddEncryptionVersion()
        Dim crypto = New AesGcmFieldCrypto(keys)
        Assert.Equal(CByte(2), crypto.CurrentKeyVersion)
        Assert.Equal("123456789012", crypto.Decrypt(before, 1, ctx))
    End Sub

    <Fact>
    Public Sub EncryptionVersions_ListsVersionsOnly()
        Dim keys = FieldKeySet.CreateNew()
        keys.AddEncryptionVersion()
        Dim versions = keys.EncryptionVersions()
        Assert.Equal(2, versions.Count)
        Assert.Equal(CByte(1), versions(0))
        Assert.Equal(CByte(2), versions(1))
    End Sub

    <Fact>
    Public Sub UnknownVersion_Throws()
        Assert.Throws(Of InvalidOperationException)(Sub() FieldKeySet.CreateNew().GetKey(FieldKeyPurpose.Encryption, 5))
    End Sub

    <Fact>
    Public Sub Deserialize_Garbage_Throws_InvalidDataException()
        Assert.Throws(Of IO.InvalidDataException)(Sub() FieldKeySet.Deserialize(New Byte() {1, 2, 3}))
        Assert.Throws(Of IO.InvalidDataException)(Sub() FieldKeySet.Deserialize(Text.Encoding.UTF8.GetBytes("RVFK1" & vbLf & "current=1" & vbLf & "1|1|AAAA" & vbLf)))
    End Sub
End Class
