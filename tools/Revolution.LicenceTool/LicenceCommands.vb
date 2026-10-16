Imports System.Globalization

''' <summary>The three commands: keygen, issue, inspect (ADR 0018). Each returns the process exit code.</summary>
Public NotInheritable Class LicenceCommands
    Private Const DateFormat As String = "yyyy-MM-dd"

    Private Sub New()
    End Sub

    Public Shared Function KeyGen(args As Arguments) As Integer
        Dim keyId = args.Need("key-id")
        Dim outFile = args.Need("out")
        If IO.File.Exists(outFile) Then
            Console.Error.WriteLine($"{outFile} already exists. An existing key file is never overwritten: choose another name.")
            Return 2
        End If
        Dim password = Prompt.ReadSecret("New password for the key file (at least 12 characters): ")
        If Not String.Equals(password, Prompt.ReadSecret("Type it again: "), StringComparison.Ordinal) Then
            Console.Error.WriteLine("The two passwords differ. Nothing was written.")
            Return 2
        End If
        Dim key = SigningKey.Generate(keyId)
        key.Save(outFile, password)
        Console.WriteLine($"Signing key '{key.KeyId}' saved to {outFile}.")
        Console.WriteLine("Keep this file and its password with you only. Back it up in two places. Never put it in the repository or give it to an Associate.")
        Console.WriteLine()
        Console.WriteLine("Public key, safe to share. Paste into LicenceKeys.Trusted in Revolution.Core and ship a new build:")
        Console.WriteLine($"  {{""{key.KeyId}"", ""{key.PublicKeyText}""}}")
        Return 0
    End Function

    Public Shared Function Issue(args As Arguments) As Integer
        Dim keyFile = args.Need("key")
        Dim outFile = args.Need("out")
        Dim associate = args.Need("associate")
        Dim installation = Guid.Parse(args.Need("installation"))
        Dim plan = ParsePlan(args.Need("plan"))
        Dim users = ParseCount(args.Need("users"), "users")
        Dim companies = ParseOptionalCount(args.Find("companies"), "companies")
        Dim modules = ParseModules(args.Find("modules"))
        Dim validFrom = If(args.Find("from") Is Nothing, DateOnly.FromDateTime(DateTime.UtcNow), ParseDate(args.Find("from"), "from"))
        Dim validTo = ParseOptionalDate(args.Find("to"), "to")
        If IO.File.Exists(outFile) Then
            Console.Error.WriteLine($"{outFile} already exists. Choose another name.")
            Return 2
        End If
        Dim key = SigningKey.Load(keyFile, Prompt.ReadSecret("Password for the signing key file: "))
        Dim issued = LicenceIssuer.Issue(key, associate, installation, plan, users, companies, modules, validFrom, validTo,
                                         If(args.Find("issued-by"), Environment.UserName), If(args.Find("note"), String.Empty))
        IO.File.WriteAllText(outFile, issued.Text)
        LogIssue(keyFile, outFile, issued.Grant)
        Console.WriteLine($"Licence written to {outFile}.")
        Console.WriteLine(Describe(issued.Grant))
        Console.WriteLine("Keep a backup copy of this file and of the issued-licences.log line, then give the file to the Associate.")
        Return 0
    End Function

    Public Shared Function Inspect(args As Arguments) As Integer
        If args.Positional.Count <> 1 Then
            Console.Error.WriteLine("Usage: inspect <licence file> [--key <signing key file>] [--installation <id>]")
            Return 2
        End If
        Dim text = IO.File.ReadAllText(args.Positional(0)).Trim()
        Dim parts = text.Split("."c)
        If parts.Length <> 2 Then
            Console.Error.WriteLine("This is not a licence file.")
            Return 1
        End If
        Dim grant = LicenceFormat.ReadPayload(LicenceFormat.FromBase64Url(parts(0)))
        Console.WriteLine(Describe(grant))
        Dim keyFile = args.Find("key")
        If keyFile Is Nothing Then
            Console.WriteLine("NOT VERIFIED. Add --key <signing key file> to check the signature.")
            Return 0
        End If
        Dim key = SigningKey.Load(keyFile, Prompt.ReadSecret("Password for the signing key file: "))
        Dim installation = If(args.Find("installation") Is Nothing, grant.InstallationId, Guid.Parse(args.Find("installation")))
        Dim trusted As New Dictionary(Of String, String) From {{key.KeyId, key.PublicKeyText}}
        Dim check = LicenceVerifier.Verify(text, trusted, installation)
        Console.WriteLine(If(check.Succeeded, "Signature and installation are valid.", "REFUSED: " & check.Message))
        Return If(check.Succeeded, 0, 1)
    End Function

    Public Shared Function Describe(grant As LicenceGrant) As String
        Return $"  licence id:   {grant.LicenceId}{vbLf}" &
               $"  signed with:  {grant.KeyId}, issued by {grant.IssuedBy} on {grant.IssuedOn.ToString(DateFormat, CultureInfo.InvariantCulture)}{vbLf}" &
               $"  Associate:    {grant.Associate}{vbLf}" &
               $"  installation: {grant.InstallationId}{vbLf}" &
               $"  plan:         {grant.PlanCap}{vbLf}" &
               $"  valid:        {grant.ValidFrom.ToString(DateFormat, CultureInfo.InvariantCulture)} to {grant.ValidTo.ToString(DateFormat, CultureInfo.InvariantCulture)} (last day of use){vbLf}" &
               $"  companies:    up to {grant.MaxCompanies}, active users: up to {grant.MaxActiveUsers}{vbLf}" &
               $"  modules:      {String.Join(", ", grant.Modules)}"
    End Function

    Private Shared Sub LogIssue(keyFile As String, outFile As String, grant As LicenceGrant)
        Dim folder = IO.Path.GetDirectoryName(IO.Path.GetFullPath(keyFile))
        Dim logFile = IO.Path.Combine(folder, "issued-licences.log")
        Dim stamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
        Dim line = $"{stamp}Z  {grant.LicenceId}  {grant.Associate}  {grant.PlanCap}  " &
                   $"{grant.ValidFrom.ToString(DateFormat, CultureInfo.InvariantCulture)}..{grant.ValidTo.ToString(DateFormat, CultureInfo.InvariantCulture)}  {outFile}"
        IO.File.AppendAllText(logFile, line & vbLf)
    End Sub

    Private Shared Function ParsePlan(text As String) As PlanTier
        Dim plan As PlanTier
        If Not System.Enum.TryParse(text, True, plan) OrElse Not System.Enum.IsDefined(GetType(PlanTier), plan) Then
            Throw New ArgumentException($"Unknown plan '{text}'. Use Copper, Silver, Gold or Platinum.")
        End If
        Return plan
    End Function

    Private Shared Function ParseCount(text As String, name As String) As Integer
        Dim count As Integer
        If Not Integer.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, count) Then
            Throw New ArgumentException($"--{name} must be a whole number.")
        End If
        Return count
    End Function

    Private Shared Function ParseOptionalCount(text As String, name As String) As Integer?
        If text Is Nothing Then
            Return Nothing
        End If
        Return ParseCount(text, name)
    End Function

    Private Shared Function ParseDate(text As String, name As String) As DateOnly
        Dim day As DateOnly
        If Not DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, day) Then
            Throw New ArgumentException($"--{name} must be a date written yyyy-MM-dd.")
        End If
        Return day
    End Function

    Private Shared Function ParseOptionalDate(text As String, name As String) As DateOnly?
        If text Is Nothing Then
            Return Nothing
        End If
        Return ParseDate(text, name)
    End Function

    ''' <summary>Nothing or "all" means every module. Otherwise a comma-separated list of module names.</summary>
    Private Shared Function ParseModules(text As String) As List(Of ModuleKey)
        Dim result As New List(Of ModuleKey)()
        If text Is Nothing OrElse String.Equals(text.Trim(), "all", StringComparison.OrdinalIgnoreCase) Then
            result.AddRange(System.Enum.GetValues(GetType(ModuleKey)).Cast(Of ModuleKey)())
            Return result
        End If
        For Each name In text.Split(","c)
            Dim licensed As ModuleKey
            If Not System.Enum.TryParse(name.Trim(), True, licensed) OrElse Not System.Enum.IsDefined(GetType(ModuleKey), licensed) Then
                Throw New ArgumentException($"Unknown module '{name.Trim()}'. Valid: {String.Join(", ", System.Enum.GetNames(GetType(ModuleKey)))}.")
            End If
            result.Add(licensed)
        Next
        Return result
    End Function
End Class
