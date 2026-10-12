Imports System.Security.Cryptography
Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class FieldKeyBackupTests
    Private Const Pwd As String = "correct horse battery"

    <Fact>
    Public Sub RoundTrip_ReturnsThePayload()
        Dim payload = RandomNumberGenerator.GetBytes(200)
        Dim backup = FieldKeyBackup.Protect(payload, Pwd)
        Assert.True(FieldKeyBackup.Unprotect(backup, Pwd).AsSpan().SequenceEqual(payload))
    End Sub

    <Fact>
    Public Sub WrongPassword_Fails()
        Dim backup = FieldKeyBackup.Protect(RandomNumberGenerator.GetBytes(50), Pwd)
        Assert.Throws(Of CryptographicException)(Sub() FieldKeyBackup.Unprotect(backup, "another password here"))
    End Sub

    <Fact>
    Public Sub ChangedByte_Fails()
        Dim backup = FieldKeyBackup.Protect(RandomNumberGenerator.GetBytes(50), Pwd)
        backup(backup.Length - 1) = CByte(backup(backup.Length - 1) Xor 1)
        Assert.Throws(Of CryptographicException)(Sub() FieldKeyBackup.Unprotect(backup, Pwd))
    End Sub

    <Fact>
    Public Sub NotABackup_Fails()
        Assert.Throws(Of CryptographicException)(Sub() FieldKeyBackup.Unprotect(New Byte(99) {}, Pwd))
        Assert.Throws(Of CryptographicException)(Sub() FieldKeyBackup.Unprotect(Nothing, Pwd))
    End Sub

    <Fact>
    Public Sub ShortPassword_IsRefused()
        Assert.Throws(Of ArgumentException)(Sub() FieldKeyBackup.Protect(New Byte() {1, 2, 3}, "short"))
    End Sub

    <Fact>
    Public Sub TwoBackups_OfTheSamePayload_Differ()
        Dim payload = RandomNumberGenerator.GetBytes(50)
        Assert.False(FieldKeyBackup.Protect(payload, Pwd).AsSpan().SequenceEqual(FieldKeyBackup.Protect(payload, Pwd)))
    End Sub

    <Fact>
    Public Sub KeySet_SurvivesBackupAndRestore()
        Dim keys = FieldKeySet.CreateNew()
        keys.AddEncryptionVersion()
        Dim restored = FieldKeySet.Deserialize(FieldKeyBackup.Unprotect(FieldKeyBackup.Protect(keys.Serialize(), Pwd), Pwd))
        Assert.Equal(CByte(2), restored.CurrentVersion)
        Assert.True(restored.GetKey(FieldKeyPurpose.Encryption, 1).AsSpan().SequenceEqual(keys.GetKey(FieldKeyPurpose.Encryption, 1)))
    End Sub
End Class
