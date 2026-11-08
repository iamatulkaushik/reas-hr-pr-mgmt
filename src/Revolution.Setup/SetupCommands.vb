Imports System.Data
Imports System.Text.RegularExpressions
Imports Microsoft.Data.SqlClient
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

    Friend Shared Function SetAssociatePlan(connection As String, args As String()) As Integer
        Dim plan As PlanTier
        If args.Length < 3 OrElse Not [Enum].TryParse(args(2), True, plan) Then
            Console.Error.WriteLine("Usage: set-associate-plan <userName> <Copper|Silver|Gold|Platinum>")
            Return 2
        End If
        Dim result = BuildProvisioning(connection).SetAssociatePlan(args(1), plan)
        If result.Succeeded Then
            Console.WriteLine($"Associate plan set to {plan}.")
            Return 0
        End If
        Console.Error.WriteLine(result.Message)
        Return 1
    End Function

    ''' <summary>
    ''' Creates the punch agent's SQL login and puts it in the database role hrms_agent (migration 0031), which can execute the agt procedures and nothing else.
    ''' The password is asked for twice and sent as a value, never written to a file or a log. Needs a login that can create logins.
    ''' </summary>
    Friend Shared Function CreateAgentLogin(connection As String, args As String()) As Integer
        If args.Length < 2 OrElse Not Regex.IsMatch(args(1), "^[A-Za-z][A-Za-z0-9_]{2,59}$") Then
            Console.Error.WriteLine("Usage: create-agent-login <loginName>   (3 to 60 letters, digits or underscores, starting with a letter)")
            Return 2
        End If
        Dim password = ConsolePrompt.ReadNewPassword()
        If password Is Nothing Then
            Console.Error.WriteLine("The two passwords did not match.")
            Return 1
        End If
        If password.Length < 12 OrElse password.Length > 128 Then
            Console.Error.WriteLine("The password must be 12 to 128 characters.")
            Return 1
        End If
        Using conn As New SqlConnection(connection)
            conn.Open()
            Using cmd = conn.CreateCommand()
                cmd.CommandText =
                    "IF DATABASE_PRINCIPAL_ID(N'hrms_agent') IS NULL THROW 53420, N'Run migrate first: the role hrms_agent does not exist.', 1;" &
                    "IF SUSER_ID(@name) IS NOT NULL THROW 53421, N'A login with this name already exists.', 1;" &
                    "DECLARE @sql NVARCHAR(MAX) = N'CREATE LOGIN ' + QUOTENAME(@name) + N' WITH PASSWORD = ' + QUOTENAME(@password, '''') + N', CHECK_POLICY = ON;';" &
                    "EXEC (@sql);" &
                    "SET @sql = N'CREATE USER ' + QUOTENAME(@name) + N' FOR LOGIN ' + QUOTENAME(@name) + N';';" &
                    "EXEC (@sql);" &
                    "SET @sql = N'ALTER ROLE hrms_agent ADD MEMBER ' + QUOTENAME(@name) + N';';" &
                    "EXEC (@sql);"
                cmd.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = args(1)
                cmd.Parameters.Add("@password", SqlDbType.NVarChar, 128).Value = password
                cmd.ExecuteNonQuery()
            End Using
        End Using
        Console.WriteLine($"Agent login {args(1)} created in the role hrms_agent. Use it in the agent's connection string.")
        Return 0
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
