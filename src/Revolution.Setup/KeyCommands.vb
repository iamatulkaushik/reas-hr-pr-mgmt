Imports System.Runtime.Versioning
Imports Revolution.Services

''' <summary>
''' Field key file commands (ADR 0011). Windows only (DPAPI). They need no database connection.
''' Prints versions and paths, never key material or passwords.
''' </summary>
<SupportedOSPlatform("windows")>
Friend NotInheritable Class KeyCommands
    Private Sub New()
    End Sub

    Friend Const Usage As String =
        "  keys-init [keyFile]                     make the field key file (refuses to overwrite)" & vbLf &
        "  keys-info [keyFile]                     show key versions in the file" & vbLf &
        "  keys-add-version [keyFile]              add a new encryption key version (then re-encrypt old rows)" & vbLf &
        "  keys-backup <backupFile> [keyFile]      write a password-protected copy of the keys" & vbLf &
        "  keys-restore <backupFile> [keyFile]     restore a backup on this computer (refuses to overwrite)"

    Friend Shared Function IsKeyCommand(name As String) As Boolean
        Return name.StartsWith("keys-", StringComparison.OrdinalIgnoreCase)
    End Function

    Friend Shared Function Run(args As String()) As Integer
        Select Case args(0).ToLowerInvariant()
            Case "keys-init"
                Dim keyFile = PathAt(args, 1)
                DpapiFieldKeyStore.Initialize(keyFile)
                Console.WriteLine("Key file created: " & keyFile)
                Console.WriteLine("Make a password backup now (keys-backup) and keep it away from this computer and from the database backups.")
                Console.WriteLine("Without the key file or a backup, every protected id is lost.")
                Return 0
            Case "keys-info"
                Dim keys = DpapiFieldKeyStore.Load(PathAt(args, 1))
                Console.WriteLine("Current encryption key version: " & keys.CurrentVersion)
                Console.WriteLine("Encryption key versions: " & String.Join(", ", keys.EncryptionVersions()))
                Return 0
            Case "keys-add-version"
                Dim version = DpapiFieldKeyStore.AddVersion(PathAt(args, 1))
                Console.WriteLine("Key version " & version & " added and now used for new values.")
                Console.WriteLine("Older rows still open. Make a new backup (keys-backup) before you go on.")
                Return 0
            Case "keys-backup"
                If args.Length < 2 Then
                    Console.Error.WriteLine("Usage: keys-backup <backupFile> [keyFile]")
                    Return 2
                End If
                Dim password = ConsolePrompt.ReadNewPassword()
                If password Is Nothing Then
                    Console.Error.WriteLine("The two passwords did not match.")
                    Return 1
                End If
                DpapiFieldKeyStore.ExportBackup(PathAt(args, 2), args(1), password)
                Console.WriteLine("Backup written: " & args(1))
                Console.WriteLine("Store the password separately. It cannot be recovered.")
                Return 0
            Case "keys-restore"
                If args.Length < 2 Then
                    Console.Error.WriteLine("Usage: keys-restore <backupFile> [keyFile]")
                    Return 2
                End If
                DpapiFieldKeyStore.Restore(args(1), ConsolePrompt.ReadSecret("Backup password: "), PathAt(args, 2))
                Console.WriteLine("Key file restored: " & PathAt(args, 2))
                Return 0
            Case Else
                Console.Error.WriteLine(Usage)
                Return 2
        End Select
    End Function

    Private Shared Function PathAt(args As String(), index As Integer) As String
        Return If(args.Length > index, args(index), DpapiFieldKeyStore.DefaultPath())
    End Function
End Class
