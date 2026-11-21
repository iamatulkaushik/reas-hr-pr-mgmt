Imports System.Globalization
Imports System.Security.Cryptography
Imports System.Text.RegularExpressions

''' <summary>
''' Signs and imports a licence for the Developer (ADR 0018, amended). It reuses the licence tool's signing code, so a licence made here and one
''' made by the tool are identical. After signing, the licence goes through the same import as a file does, so it must verify against a trusted
''' key for this installation. A backup copy and a log line are saved beside the key file (the Developer's backup duty).
''' </summary>
Public NotInheritable Class LicenceIssueService
    Implements ILicenceIssueService

    Private Const MaxNoteLength As Integer = 200
    Private Const MaxNameLength As Integer = 100
    Private Const MaxUsersCeiling As Integer = 100000
    Private Const MaxCompaniesCeiling As Integer = 500
    Private Const MinPasswordLength As Integer = 12
    Private Const LogFileName As String = "issued-licences.log"
    Private Const BackupFolder As String = "licences"

    Private Shared ReadOnly KeyIdPattern As New Regex("^[A-Za-z0-9._-]{1,40}$", RegexOptions.CultureInvariant)

    Private ReadOnly _admin As ILicenceAdminService
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _audit As IAuditService

    Public Sub New(admin As ILicenceAdminService, permissions As IPermissionService, session As ISessionContext, audit As IAuditService)
        If admin Is Nothing Then Throw New ArgumentNullException(NameOf(admin))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        _admin = admin
        _permissions = permissions
        _session = session
        _audit = audit
    End Sub

    Public Function CreateSigningKey(keyId As String, filePath As String, password As String) As OperationResult(Of SigningKeyInfo) Implements ILicenceIssueService.CreateSigningKey
        If Not _permissions.IsAllowed(ModuleKey.Licence, PermissionAction.Create) Then
            Return OperationResult(Of SigningKeyInfo).Fail("You do not have permission to create a signing key.")
        End If
        Dim id = If(keyId, String.Empty).Trim()
        If Not KeyIdPattern.IsMatch(id) Then
            Return OperationResult(Of SigningKeyInfo).Fail("The key id is 1 to 40 letters, digits, dots, dashes or underscores.")
        End If
        Dim path = If(filePath, String.Empty).Trim()
        If path.Length = 0 Then
            Return OperationResult(Of SigningKeyInfo).Fail("Choose where to save the key file.")
        End If
        If IO.File.Exists(path) Then
            Return OperationResult(Of SigningKeyInfo).Fail("That file already exists. An existing key file is never overwritten: choose another name.")
        End If
        If If(password, String.Empty).Length < MinPasswordLength Then
            Return OperationResult(Of SigningKeyInfo).Fail($"The password needs at least {MinPasswordLength} characters.")
        End If
        Try
            IO.Directory.CreateDirectory(IO.Path.GetDirectoryName(IO.Path.GetFullPath(path)))
            Dim key = SigningKey.Generate(id)
            key.Save(path, password)
            _audit.Record(New AuditEntry(ModuleKey.Licence, "SigningKeyCreated", "SigningKey", key.KeyId))
            Return OperationResult(Of SigningKeyInfo).Ok(New SigningKeyInfo(key.KeyId, key.PublicKeyText, path))
        Catch ex As ArgumentException
            Return OperationResult(Of SigningKeyInfo).Fail("The key file could not be saved: " & ex.Message)
        Catch ex As IO.IOException
            Return OperationResult(Of SigningKeyInfo).Fail("The key file could not be saved: " & ex.Message)
        Catch ex As UnauthorizedAccessException
            Return OperationResult(Of SigningKeyInfo).Fail("The key file could not be saved: " & ex.Message)
        Catch ex As NotSupportedException
            Return OperationResult(Of SigningKeyInfo).Fail("The key file could not be saved: " & ex.Message)
        End Try
    End Function

    Public Function Issue(request As LicenceIssueRequest) As OperationResult(Of LicenceIssueResult) Implements ILicenceIssueService.Issue
        If request Is Nothing Then Throw New ArgumentNullException(NameOf(request))
        If Not _permissions.IsAllowed(ModuleKey.Licence, PermissionAction.Create) Then
            Return OperationResult(Of LicenceIssueResult).Fail("You do not have permission to issue licences.")
        End If
        Dim problem = Validate(request)
        If problem.Length > 0 Then
            Return OperationResult(Of LicenceIssueResult).Fail(problem)
        End If

        Dim key As SigningKey = Nothing
        Dim opened = OpenKey(request.KeyFilePath, request.KeyPassword, key)
        If Not opened.Succeeded Then
            Return OperationResult(Of LicenceIssueResult).Fail(opened.Message)
        End If

        Dim associate = request.AssociateUserName.Trim()
        Dim issued As IssuedLicence = Nothing
        Try
            Dim allModules = System.Enum.GetValues(GetType(ModuleKey)).Cast(Of ModuleKey)().ToList()
            issued = LicenceIssuer.Issue(key, associate, _admin.GetInstallationId(), request.Plan, request.MaxActiveUsers, request.MaxCompanies,
                                         allModules, request.ValidFrom, request.ValidTo, IssuerName(), If(request.Note, String.Empty).Trim())
        Catch ex As ArgumentException
            Return OperationResult(Of LicenceIssueResult).Fail(ex.Message)
        End Try

        Dim imported = _admin.Import(issued.Text)
        If Not imported.Succeeded Then
            Return OperationResult(Of LicenceIssueResult).Fail(imported.Message)
        End If

        Dim grant = issued.Grant
        Dim backup As String = String.Empty
        Dim warning As String = String.Empty
        Try
            backup = SaveBackup(request.KeyFilePath, grant, issued.Text)
        Catch ex As IO.IOException
            warning = "The licence is issued and imported, but the backup copy could not be saved: " & ex.Message
        Catch ex As UnauthorizedAccessException
            warning = "The licence is issued and imported, but the backup copy could not be saved: " & ex.Message
        End Try
        _audit.Record(New AuditEntry(ModuleKey.Licence, "LicenceIssued", "Licence", grant.LicenceId.ToString(), $"{grant.Associate}, {grant.PlanCap}"))
        Return OperationResult(Of LicenceIssueResult).Ok(New LicenceIssueResult(grant.LicenceId, Describe(grant), backup, warning))
    End Function

    Private Shared Function Validate(request As LicenceIssueRequest) As String
        Dim associate = If(request.AssociateUserName, String.Empty).Trim()
        If associate.Length = 0 OrElse associate.Length > MaxNameLength OrElse associate.Any(Function(c) Char.IsWhiteSpace(c)) Then
            Return "Enter the Associate's user name exactly as it was created, with no spaces."
        End If
        If Not System.Enum.IsDefined(GetType(PlanTier), request.Plan) Then
            Return "Choose a plan."
        End If
        If request.MaxActiveUsers < 1 OrElse request.MaxActiveUsers > MaxUsersCeiling Then
            Return $"Active users must be 1 to {MaxUsersCeiling.ToString(CultureInfo.InvariantCulture)}."
        End If
        If request.MaxCompanies.HasValue AndAlso (request.MaxCompanies.Value < 1 OrElse request.MaxCompanies.Value > MaxCompaniesCeiling) Then
            Return $"Companies must be 1 to {MaxCompaniesCeiling.ToString(CultureInfo.InvariantCulture)}, or left empty for the plan's quota."
        End If
        If request.ValidTo.HasValue AndAlso request.ValidTo.Value < request.ValidFrom Then
            Return "The last day of use cannot be before the first day."
        End If
        If If(request.Note, String.Empty).Length > MaxNoteLength Then
            Return $"The note can be at most {MaxNoteLength.ToString(CultureInfo.InvariantCulture)} characters."
        End If
        If String.IsNullOrWhiteSpace(request.KeyFilePath) Then
            Return "Choose the signing key file."
        End If
        If String.IsNullOrEmpty(request.KeyPassword) Then
            Return "Enter the password of the signing key file."
        End If
        Return String.Empty
    End Function

    Private Shared Function OpenKey(filePath As String, password As String, ByRef key As SigningKey) As OperationResult
        Try
            key = SigningKey.Load(filePath.Trim(), password)
            Return OperationResult.Ok()
        Catch ex As CryptographicException
            Return OperationResult.Fail("The key file could not be opened: wrong password, or this is not a signing key file.")
        Catch ex As FormatException
            Return OperationResult.Fail("The key file could not be opened: this is not a signing key file.")
        Catch ex As IO.IOException
            Return OperationResult.Fail("The key file could not be read: " & ex.Message)
        Catch ex As UnauthorizedAccessException
            Return OperationResult.Fail("The key file could not be read: " & ex.Message)
        End Try
    End Function

    Private Function IssuerName() As String
        Dim current = _session.Current
        Return If(current Is Nothing, String.Empty, current.UserName)
    End Function

    Private Shared Function SaveBackup(keyFilePath As String, grant As LicenceGrant, licenceText As String) As String
        Dim keyFolder = IO.Path.GetDirectoryName(IO.Path.GetFullPath(keyFilePath.Trim()))
        Dim folder = IO.Path.Combine(keyFolder, BackupFolder)
        IO.Directory.CreateDirectory(folder)
        Dim invalid = IO.Path.GetInvalidFileNameChars()
        Dim safeName = String.Concat(grant.Associate.Select(Function(c) If(invalid.Contains(c), "_"c, c)))
        Dim fileName = $"{safeName}_{grant.ValidFrom.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}_{grant.LicenceId.ToString("N").Substring(0, 8)}.lic"
        Dim backup = IO.Path.Combine(folder, fileName)
        IO.File.WriteAllText(backup, licenceText)
        Dim stamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
        Dim line = $"{stamp}Z  {grant.LicenceId}  {grant.Associate}  {grant.PlanCap}  " &
                   $"{grant.ValidFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}..{grant.ValidTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}  {backup}"
        IO.File.AppendAllText(IO.Path.Combine(keyFolder, LogFileName), line & vbLf)
        Return backup
    End Function

    Private Shared Function Describe(grant As LicenceGrant) As String
        Return $"{grant.Associate}: {grant.PlanCap}, up to {grant.MaxActiveUsers.ToString(CultureInfo.InvariantCulture)} users and {grant.MaxCompanies.ToString(CultureInfo.InvariantCulture)} companies, " &
               $"{grant.ValidFrom.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)} to {grant.ValidTo.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)}."
    End Function
End Class
