''' <summary>Setup tool: applies migrations and creates the first companies and users. Run by the installer or an administrator.</summary>
Public Module Program
    Private Const Usage As String =
        "RevolutionSetup <command>" & vbLf &
        "  migrate [folder]                        apply database/migrations (default folder: database/migrations)" & vbLf &
        "  add-company <name> <plan>               plan: Copper | Silver | Gold | Platinum" & vbLf &
        "  add-user <userName> <role> [companyId]  role: Developer | Owner | Associate | Operator" & vbLf &
        "Set HRMS_SETUP_CONNECTION to an administrator connection string for the HRMS database."

    Public Function Main(args As String()) As Integer
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
                Case Else
                    Console.Error.WriteLine(Usage)
                    Return 2
            End Select
        Catch ex As Exception
            Console.Error.WriteLine("Failed: " & ex.Message)
            Return 1
        End Try
    End Function
End Module
