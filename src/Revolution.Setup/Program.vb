''' <summary>Setup tool: applies migrations and creates the first companies and users. Run by the installer or an administrator.</summary>
Public Module Program
    Private Const Usage As String =
        "RevolutionSetup <command>" & vbLf &
        "  migrate [folder]                        apply database/migrations (default folder: database/migrations)" & vbLf &
        "  add-company <name> <plan>               plan: Copper | Silver | Gold | Platinum" & vbLf &
        "  add-user <userName> <role> [companyId]  role: Developer | Owner | Associate | Operator (an Associate may have no company)" & vbLf &
        "  set-associate-plan <userName> <plan>    caps companies per Associate: Copper 5, Silver 15, Gold 35, Platinum 90" & vbLf &
        "Field key commands (Windows only, no database needed):" & vbLf &
        KeyCommandsUsage & vbLf &
        "Set HRMS_SETUP_CONNECTION to an administrator connection string for the HRMS database."

    Private Const KeyCommandsUsage As String =
        "  keys-init | keys-info | keys-add-version | keys-backup <file> | keys-restore <file>   (see ADR 0011)"

    Public Function Main(args As String()) As Integer
        If args.Length > 0 AndAlso args(0).StartsWith("keys-", StringComparison.OrdinalIgnoreCase) Then
            Return RunKeyCommand(args)
        End If
        Dim connection = Environment.GetEnvironmentVariable("HRMS_SETUP_CONNECTION")
        If args.Length = 0 OrElse String.IsNullOrWhiteSpace(connection) Then
            Console.Error.WriteLine(Usage)
            Return 2
        End If
        Try
            Select Case args(0).ToLowerInvariant()
                Case "migrate"
                    Return SetupCommands.Migrate(connection, args)
                Case "add-company"
                    Return SetupCommands.AddCompany(connection, args)
                Case "add-user"
                    Return SetupCommands.AddUser(connection, args)
                Case "set-associate-plan"
                    Return SetupCommands.SetAssociatePlan(connection, args)
                Case Else
                    Console.Error.WriteLine(Usage)
                    Return 2
            End Select
        Catch ex As Exception
            Console.Error.WriteLine("Failed: " & ex.Message)
            Return 1
        End Try
    End Function

    Private Function RunKeyCommand(args As String()) As Integer
        If Not OperatingSystem.IsWindows() Then
            Console.Error.WriteLine("The key commands run on Windows only (DPAPI).")
            Return 1
        End If
        Try
            Return KeyCommands.Run(args)
        Catch ex As Exception
            Console.Error.WriteLine("Failed: " & ex.Message)
            Return 1
        End Try
    End Function
End Module
