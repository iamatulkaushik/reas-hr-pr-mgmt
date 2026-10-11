Imports System.Security.Cryptography
Imports System.Text
Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class FieldCryptoTests
    Private Shared Function Make(Optional keys As FakeFieldKeyProvider = Nothing) As AesGcmFieldCrypto
        Return New AesGcmFieldCrypto(If(keys, New FakeFieldKeyProvider()))
    End Function

    Private Shared ReadOnly Ctx As String = FieldContext.Build(7, 42, FieldContext.Pan)

    <Fact>
    Public Sub RoundTrip_ReturnsTheSameText()
        Dim crypto = Make()
        Dim cipher = crypto.Encrypt("ABCDE1234F", Ctx)
        Assert.Equal("ABCDE1234F", crypto.Decrypt(cipher, crypto.CurrentKeyVersion, Ctx))
    End Sub

    <Fact>
    Public Sub Encrypt_SameValueTwice_GivesDifferentBytes()
        Dim crypto = Make()
        Dim a = Convert.ToBase64String(crypto.Encrypt("ABCDE1234F", Ctx))
        Dim b = Convert.ToBase64String(crypto.Encrypt("ABCDE1234F", Ctx))
        Assert.False(a = b)
    End Sub

    <Fact>
    Public Sub Cipher_HoldsNoPlainText()
        Dim crypto = Make()
        Dim cipher = crypto.Encrypt("ABCDE1234F", Ctx)
        Dim plain = Encoding.UTF8.GetBytes("ABCDE1234F")
        Dim found = False
        For i = 0 To cipher.Length - plain.Length
            If cipher.AsSpan(i, plain.Length).SequenceEqual(plain) Then found = True
        Next
        Assert.False(found)
    End Sub

    <Fact>
    Public Sub Cipher_FitsTheColumn_ForTheLongestIds()
        Dim crypto = Make()
        Assert.True(crypto.Encrypt(New String("9"c, 50), Ctx).Length <= 256)
        Assert.Equal(12 + 16 + 10, crypto.Encrypt("ABCDE1234F", Ctx).Length)
    End Sub

    <Fact>
    Public Sub Decrypt_ChangedByte_Fails()
        Dim crypto = Make()
        Dim cipher = crypto.Encrypt("123456789012", FieldContext.Build(7, 42, FieldContext.Aadhaar))
        cipher(cipher.Length - 1) = CByte(cipher(cipher.Length - 1) Xor 1)
        Assert.Throws(Of CryptographicException)(Sub() crypto.Decrypt(cipher, crypto.CurrentKeyVersion, FieldContext.Build(7, 42, FieldContext.Aadhaar)))
    End Sub

    <Theory>
    <InlineData(8, 42, "PAN")>
    <InlineData(7, 43, "PAN")>
    <InlineData(7, 42, "Aadhaar")>
    Public Sub Decrypt_WrongCompanyEmployeeOrField_Fails(company As Integer, employee As Integer, field As String)
        Dim crypto = Make()
        Dim cipher = crypto.Encrypt("ABCDE1234F", Ctx)
        Assert.Throws(Of CryptographicException)(Sub() crypto.Decrypt(cipher, crypto.CurrentKeyVersion, FieldContext.Build(company, employee, field)))
    End Sub

    <Fact>
    Public Sub Decrypt_TooShort_Fails()
        Dim crypto = Make()
        Assert.Throws(Of CryptographicException)(Sub() crypto.Decrypt(New Byte(10) {}, 1, Ctx))
        Assert.Throws(Of CryptographicException)(Sub() crypto.Decrypt(Nothing, 1, Ctx))
    End Sub

    <Fact>
    Public Sub OldKeyVersion_StillDecrypts_AfterRotation()
        Dim keys As New FakeFieldKeyProvider(1)
        Dim before = Make(keys).Encrypt("ABCDE1234F", Ctx)
        Dim rotated As New FakeFieldKeyProvider(2)
        rotated.SetKey(FieldKeyPurpose.Encryption, 1, keys.GetKey(FieldKeyPurpose.Encryption, 1))
        rotated.SetKey(FieldKeyPurpose.BlindIndex, 1, keys.GetKey(FieldKeyPurpose.BlindIndex, 1))
        Dim crypto = Make(rotated)
        Assert.Equal(CByte(2), crypto.CurrentKeyVersion)
        Assert.Equal("ABCDE1234F", crypto.Decrypt(before, 1, Ctx))
    End Sub

    <Fact>
    Public Sub MissingKeyVersion_Throws()
        Dim crypto = Make()
        Dim cipher = crypto.Encrypt("ABCDE1234F", Ctx)
        Assert.Throws(Of InvalidOperationException)(Sub() crypto.Decrypt(cipher, 9, Ctx))
    End Sub

    <Fact>
    Public Sub KeyOfWrongLength_Throws()
        Dim keys As New FakeFieldKeyProvider()
        keys.SetKey(FieldKeyPurpose.Encryption, 1, New Byte(15) {})
        Assert.Throws(Of InvalidOperationException)(Sub() Make(keys).Encrypt("ABCDE1234F", Ctx))
    End Sub

    <Fact>
    Public Sub Encrypt_NothingOrNoContext_Throws()
        Dim crypto = Make()
        Assert.Throws(Of ArgumentException)(Sub() crypto.Encrypt("", Ctx))
        Assert.Throws(Of ArgumentException)(Sub() crypto.Encrypt("ABCDE1234F", ""))
    End Sub

    <Fact>
    Public Sub BlindIndex_SameInput_SameOutput_32Bytes()
        Dim crypto = Make()
        Dim a = crypto.BlindIndex(7, FieldContext.Pan, "ABCDE1234F")
        Dim b = crypto.BlindIndex(7, FieldContext.Pan, "ABCDE1234F")
        Assert.Equal(32, a.Length)
        Assert.True(a.AsSpan().SequenceEqual(b))
    End Sub

    <Theory>
    <InlineData("abcde1234f")>
    <InlineData(" ABCDE-1234F ")>
    <InlineData("ABCDE 1234 F")>
    Public Sub BlindIndex_IgnoresCaseSpacesAndHyphens(typed As String)
        Dim crypto = Make()
        Assert.True(crypto.BlindIndex(7, FieldContext.Pan, typed).AsSpan().SequenceEqual(crypto.BlindIndex(7, FieldContext.Pan, "ABCDE1234F")))
    End Sub

    <Fact>
    Public Sub BlindIndex_DiffersByCompanyAndField()
        Dim crypto = Make()
        Dim baseValue = crypto.BlindIndex(7, FieldContext.Pan, "ABCDE1234F")
        Assert.False(baseValue.AsSpan().SequenceEqual(crypto.BlindIndex(8, FieldContext.Pan, "ABCDE1234F")))
        Assert.False(baseValue.AsSpan().SequenceEqual(crypto.BlindIndex(7, FieldContext.Uan, "ABCDE1234F")))
    End Sub

    <Fact>
    Public Sub BlindIndex_DoesNotChange_WhenEncryptionKeyRotates()
        Dim keys As New FakeFieldKeyProvider(1)
        Dim before = Make(keys).BlindIndex(7, FieldContext.Pan, "ABCDE1234F")
        Dim rotated As New FakeFieldKeyProvider(2)
        rotated.SetKey(FieldKeyPurpose.BlindIndex, 1, keys.GetKey(FieldKeyPurpose.BlindIndex, 1))
        Assert.True(before.AsSpan().SequenceEqual(Make(rotated).BlindIndex(7, FieldContext.Pan, "ABCDE1234F")))
    End Sub

    <Fact>
    Public Sub BlindIndex_NothingToIndex_Throws()
        Dim crypto = Make()
        Assert.Throws(Of ArgumentException)(Sub() crypto.BlindIndex(7, FieldContext.Pan, " "))
        Assert.Throws(Of ArgumentException)(Sub() crypto.BlindIndex(7, "", "ABCDE1234F"))
    End Sub

    <Fact>
    Public Sub FieldContext_NeedsAFieldName()
        Assert.Throws(Of ArgumentException)(Sub() FieldContext.Build(1, 2, " "))
        Assert.Equal("7:42:PAN", FieldContext.Build(7, 42, FieldContext.Pan))
    End Sub
End Class
