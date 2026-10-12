Imports System.IO
Imports System.Runtime.Versioning
Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

''' <summary>Windows only: DPAPI does not exist elsewhere. On other systems each test returns at once. CI runs on Windows.</summary>
<SupportedOSPlatform("windows")>
Public Class DpapiFieldKeyStoreTests
    Private Shared Function TempFile() As String
        Return Path.Combine(Path.GetTempPath(), "rv-keys-" & Guid.NewGuid().ToString("N"), "fieldkeys.dat")
    End Function

    <Fact>
    Public Sub Initialize_ThenLoad_GivesVersionOne()
        If Not OperatingSystem.IsWindows() Then Return
        Dim keyFile = TempFile()
        DpapiFieldKeyStore.Initialize(keyFile)
        Assert.Equal(CByte(1), DpapiFieldKeyStore.Load(keyFile).CurrentVersion)
    End Sub

    <Fact>
    Public Sub FileOnDisk_IsNotTheRawKeyText()
        If Not OperatingSystem.IsWindows() Then Return
        Dim keyFile = TempFile()
        DpapiFieldKeyStore.Initialize(keyFile)
        Assert.False(Text.Encoding.UTF8.GetString(File.ReadAllBytes(keyFile)).Contains("RVFK1"))
    End Sub

    <Fact>
    Public Sub Initialize_RefusesToOverwrite()
        If Not OperatingSystem.IsWindows() Then Return
        Dim keyFile = TempFile()
        DpapiFieldKeyStore.Initialize(keyFile)
        Assert.Throws(Of InvalidOperationException)(Sub() DpapiFieldKeyStore.Initialize(keyFile))
    End Sub

    <Fact>
    Public Sub AddVersion_KeepsOldOnes()
        If Not OperatingSystem.IsWindows() Then Return
        Dim keyFile = TempFile()
        DpapiFieldKeyStore.Initialize(keyFile)
        Dim v1 = DpapiFieldKeyStore.Load(keyFile).GetKey(FieldKeyPurpose.Encryption, 1).ToArray()
        Assert.Equal(CByte(2), DpapiFieldKeyStore.AddVersion(keyFile))
        Dim loaded = DpapiFieldKeyStore.Load(keyFile)
        Assert.Equal(CByte(2), loaded.CurrentVersion)
        Assert.True(loaded.GetKey(FieldKeyPurpose.Encryption, 1).AsSpan().SequenceEqual(v1))
    End Sub

    <Fact>
    Public Sub Backup_ThenRestore_OnANewPath_GivesTheSameKeys()
        If Not OperatingSystem.IsWindows() Then Return
        Dim keyFile = TempFile()
        Dim backup = Path.Combine(Path.GetTempPath(), "rv-bak-" & Guid.NewGuid().ToString("N") & ".bak")
        DpapiFieldKeyStore.Initialize(keyFile)
        DpapiFieldKeyStore.ExportBackup(keyFile, backup, "correct horse battery")
        Dim other = TempFile()
        DpapiFieldKeyStore.Restore(backup, "correct horse battery", other)
        Assert.True(DpapiFieldKeyStore.Load(other).GetKey(FieldKeyPurpose.BlindIndex, 1).AsSpan().SequenceEqual(DpapiFieldKeyStore.Load(keyFile).GetKey(FieldKeyPurpose.BlindIndex, 1)))
        Assert.Throws(Of InvalidOperationException)(Sub() DpapiFieldKeyStore.Restore(backup, "correct horse battery", other))
    End Sub

    <Fact>
    Public Sub Load_MissingFile_Throws()
        If Not OperatingSystem.IsWindows() Then Return
        Assert.Throws(Of FileNotFoundException)(Sub() DpapiFieldKeyStore.Load(TempFile()))
    End Sub
End Class
