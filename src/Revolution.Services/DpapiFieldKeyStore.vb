Imports System.IO
Imports System.Runtime.Versioning
Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' The key file (ADR 0011): the key set protected with Windows DPAPI, machine scope, so only this computer can open it.
''' Windows only. Create it with Setup, keep it OUT of the data and backup folders, and keep a password backup somewhere else.
''' Losing the file without a backup makes every protected id unreadable.
''' </summary>
<SupportedOSPlatform("windows")>
Public NotInheritable Class DpapiFieldKeyStore
    Private Shared ReadOnly Entropy As Byte() = Encoding.UTF8.GetBytes("RevolutionHRMS.FieldKeys.v1")

    Private Sub New()
    End Sub

    ''' <summary>The default folder: C:\ProgramData\RevolutionHRMS\keys\fieldkeys.dat.</summary>
    Public Shared Function DefaultPath() As String
        Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RevolutionHRMS", "keys", "fieldkeys.dat")
    End Function

    ''' <summary>Makes a new key file. Refuses to overwrite one: that would orphan every stored id.</summary>
    Public Shared Sub Initialize(path As String)
        If File.Exists(path) Then
            Throw New InvalidOperationException("A key file already exists. It was not changed.")
        End If
        Save(path, FieldKeySet.CreateNew())
    End Sub

    Public Shared Function Load(path As String) As FieldKeySet
        If Not File.Exists(path) Then
            Throw New FileNotFoundException("The key file was not found. Run the key setup first.")
        End If
        Try
            Return FieldKeySet.Deserialize(ProtectedData.Unprotect(File.ReadAllBytes(path), Entropy, DataProtectionScope.LocalMachine))
        Catch ex As CryptographicException
            Throw New CryptographicException("The key file could not be opened on this computer.")
        End Try
    End Function

    ''' <summary>Adds a new encryption key version and makes it current. Old versions stay in the file.</summary>
    Public Shared Function AddVersion(path As String) As Byte
        Dim keys = Load(path)
        Dim version = keys.AddEncryptionVersion()
        Save(path, keys)
        Return version
    End Function

    Public Shared Sub ExportBackup(path As String, backupPath As String, password As String)
        Dim keys = Load(path)
        File.WriteAllBytes(backupPath, FieldKeyBackup.Protect(keys.Serialize(), password))
    End Sub

    ''' <summary>Opens a password backup and writes it as the key file of this computer. Refuses to overwrite an existing key file.</summary>
    Public Shared Sub Restore(backupPath As String, password As String, path As String)
        If File.Exists(path) Then
            Throw New InvalidOperationException("A key file already exists. It was not changed.")
        End If
        Save(path, FieldKeySet.Deserialize(FieldKeyBackup.Unprotect(File.ReadAllBytes(backupPath), password)))
    End Sub

    Private Shared Sub Save(path As String, keys As FieldKeySet)
        Dim folder = System.IO.Path.GetDirectoryName(path)
        If Not String.IsNullOrEmpty(folder) Then
            Directory.CreateDirectory(folder)
        End If
        Dim temp = path & ".tmp"
        File.WriteAllBytes(temp, ProtectedData.Protect(keys.Serialize(), Entropy, DataProtectionScope.LocalMachine))
        File.Move(temp, path, True)
    End Sub
End Class
