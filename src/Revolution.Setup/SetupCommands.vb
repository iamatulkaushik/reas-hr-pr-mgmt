Imports Revolution.Data
Imports Revolution.Services

Friend NotInheritable Class SetupCommands
    Private Sub New()
    End Sub

    Friend Shared Function Migrate(connection As String, args As String()) As Integer
        Dim folder = If(args.Length > 1, args(1), IO.Path.Combine("database", "migrations"))
        Dim applied = New MigrationRunner(connection).Run(folder)
        Console.WriteLine($"Done. {applied} script(s) applied.")
        Return 0
    End Function

    Friend Shared Function AddCompany(connection As String, args As String()) As Integer
        Dim plan As PlanTier
        If args.Length < 3 OrElse Not [Enum].TryParse(args(2), True, plan) Then
            Console.Error.WriteLine("Usage: add-company <name> <Copper|Silver|Gold|Platinum>")
            Return 2
        End If
        Dim result = BuildProvisioning(connection).CreateCompany(args(1), plan)
        Return Report(result, "Company created with id")
    End Function

    Friend Shared Function AddUser(connection As String, args As String()) As Integer
        Dim role As UserRole
        If args.Length < 3 OrElse Not [Enum].TryParse(args(2), True, role) Then
            Console.Error.WriteLine("Usage: add-user <userName> <Developer|Owner|Associate|Operator> [companyId]")
            Return 2
        End If
        Dim companyId As Integer? = Nothing
        If args.Length > 3 Then
            Dim parsed As Integer
            If Not Integer.TryParse(args(3), parsed) Then
                Console.Error.WriteLine("companyId must be a number.")
                Return 2
            End If
            companyId = parsed
        End If
        Dim password = ConsolePrompt.ReadNewPassword()
        If password Is Nothing Then
            Console.Error.WriteLine("The two passwords did not match.")
            Return 1
        End If
        Dim result = BuildProvisioning(connection).CreateUser(args(1), password, role, companyId)
        Return Report(result, "User created with id")
    End Function

    Private Shared Function Report(result As OperationResult(Of Integer), successText As String) As Integer
        If result.Succeeded Then
            Console.WriteLine($"{successText} {result.Value}.")
            Return 0
        End If
        Console.Error.WriteLine(result.Message)
        Return 1
    End Function

    Private Shared Function BuildProvisioning(connection As String) As ProvisioningService
        Dim factory As New SqlConnectionFactory(connection, New SessionContext())
        Return New ProvisioningService(New SqlUserRepository(factory), New SqlCompanyRepository(factory),
                                       New SqlProvisioningRepository(factory), New PasswordHasher())
    End Function
End Class
