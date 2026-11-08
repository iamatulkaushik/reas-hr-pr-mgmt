''' <summary>
''' Resets another user's password to a temporary one. Checks permission and the password policy, hashes the password and records an audit entry.
''' The database separately checks that the caller may reset that user (sec.fn_ResettableUsers).
''' </summary>
Public NotInheritable Class UserPasswordAdminService
    Implements IUserPasswordAdminService

    Private ReadOnly _repository As IUserPasswordAdminRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _hasher As PasswordHasher
    Private ReadOnly _audit As IAuditService

    Public Sub New(repository As IUserPasswordAdminRepository, permissions As IPermissionService, hasher As PasswordHasher, audit As IAuditService)
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If hasher Is Nothing Then Throw New ArgumentNullException(NameOf(hasher))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        _repository = repository
        _permissions = permissions
        _hasher = hasher
        _audit = audit
    End Sub

    Public Function ListResettable() As OperationResult(Of IReadOnlyList(Of ResettableUser)) Implements IUserPasswordAdminService.ListResettable
        If Not _permissions.IsAllowed(ModuleKey.Security, PermissionAction.Edit) Then
            Return OperationResult(Of IReadOnlyList(Of ResettableUser)).Fail("You do not have permission to reset passwords.")
        End If
        Return _repository.ListResettable()
    End Function

    Public Function ResetPassword(targetUserId As Integer, temporaryPassword As String) As OperationResult Implements IUserPasswordAdminService.ResetPassword
        If Not _permissions.IsAllowed(ModuleKey.Security, PermissionAction.Edit) Then
            Return OperationResult.Fail("You do not have permission to reset passwords.")
        End If
        If targetUserId <= 0 Then
            Return OperationResult.Fail("Choose a user first.")
        End If
        Dim policy = PasswordPolicy.Validate(temporaryPassword)
        If Not policy.Succeeded Then
            Return policy
        End If
        Dim hashed = _hasher.Create(temporaryPassword)
        Dim result = _repository.ResetPassword(targetUserId, hashed.Hash, hashed.Salt, hashed.Iterations)
        If result.Succeeded Then
            _audit.Record(New AuditEntry(ModuleKey.Security, "PasswordReset", "User", targetUserId.ToString(Globalization.CultureInfo.InvariantCulture),
                                         "Temporary password set; the user must change it at the next sign-in."))
        End If
        Return result
    End Function
End Class
