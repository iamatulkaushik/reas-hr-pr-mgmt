''' <summary>
''' Creates companies and Owner logins for an Associate. Checks permission and input, hashes the temporary password and records an
''' audit entry. The database separately checks that the caller really is an Associate who works on the company.
''' </summary>
Public NotInheritable Class AssociateAdminService
    Implements IAssociateAdminService

    Private Const MaxCompanyNameLength As Integer = 200
    Private Const MinUserNameLength As Integer = 3
    Private Const MaxUserNameLength As Integer = 100

    Private ReadOnly _repository As IAssociateAdminRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _hasher As PasswordHasher
    Private ReadOnly _audit As IAuditService

    Public Sub New(repository As IAssociateAdminRepository, permissions As IPermissionService, hasher As PasswordHasher, audit As IAuditService)
        If repository Is Nothing Then
            Throw New ArgumentNullException(NameOf(repository))
        End If
        If permissions Is Nothing Then
            Throw New ArgumentNullException(NameOf(permissions))
        End If
        If hasher Is Nothing Then
            Throw New ArgumentNullException(NameOf(hasher))
        End If
        If audit Is Nothing Then
            Throw New ArgumentNullException(NameOf(audit))
        End If
        _repository = repository
        _permissions = permissions
        _hasher = hasher
        _audit = audit
    End Sub

    Public Function CreateCompany(companyName As String) As OperationResult(Of Integer) Implements IAssociateAdminService.CreateCompany
        If Not _permissions.IsAllowed(ModuleKey.Security, PermissionAction.Create) Then
            Return OperationResult(Of Integer).Fail("You do not have permission to create a company.")
        End If
        Dim name = If(companyName, String.Empty).Trim()
        If name.Length = 0 OrElse name.Length > MaxCompanyNameLength Then
            Return OperationResult(Of Integer).Fail($"A company needs a name of up to {MaxCompanyNameLength} characters.")
        End If
        Dim created = _repository.CreateCompany(name)
        If created.Succeeded Then
            _audit.Record(New AuditEntry(ModuleKey.Security, "CompanyCreated", "Company", created.Value.ToString(Globalization.CultureInfo.InvariantCulture), name))
        End If
        Return created
    End Function

    Public Function CreateOwner(companyId As Integer, userName As String, temporaryPassword As String) As OperationResult(Of Integer) Implements IAssociateAdminService.CreateOwner
        If Not _permissions.IsAllowed(ModuleKey.Security, PermissionAction.Create) Then
            Return OperationResult(Of Integer).Fail("You do not have permission to create an Owner login.")
        End If
        If companyId <= 0 Then
            Return OperationResult(Of Integer).Fail("Choose a company first.")
        End If
        Dim name = If(userName, String.Empty).Trim()
        If name.Length < MinUserNameLength OrElse name.Length > MaxUserNameLength OrElse name.Any(Function(c) Char.IsWhiteSpace(c)) Then
            Return OperationResult(Of Integer).Fail($"A user name is {MinUserNameLength} to {MaxUserNameLength} characters with no spaces.")
        End If
        Dim policy = PasswordPolicy.Validate(temporaryPassword)
        If Not policy.Succeeded Then
            Return OperationResult(Of Integer).Fail(policy.Message)
        End If
        Dim hashed = _hasher.Create(temporaryPassword)
        Dim created = _repository.CreateOwner(companyId, name, hashed.Hash, hashed.Salt, hashed.Iterations)
        If created.Succeeded Then
            _audit.Record(New AuditEntry(ModuleKey.Security, "OwnerCreated", "User", name, $"Company {companyId.ToString(Globalization.CultureInfo.InvariantCulture)}"))
        End If
        Return created
    End Function
End Class
