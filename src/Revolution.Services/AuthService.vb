''' <summary>Login, lockout and password change. Failure messages never say whether the user name exists.</summary>
Public NotInheritable Class AuthService
    Implements IAuthService

    Public Const MaxFailedAttempts As Integer = 5
    Public Const LockoutMinutes As Integer = 15
    Private Const InvalidCredentials As String = "Invalid user name or password."
    Private Const WrongCurrentPassword As String = "The current password is incorrect."
    Private Const LockedMessage As String = "This account is locked. Try again later."

    Private ReadOnly _users As IUserRepository
    Private ReadOnly _companies As ICompanyRepository
    Private ReadOnly _hasher As PasswordHasher
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _time As TimeProvider

    Public Sub New(users As IUserRepository, companies As ICompanyRepository, hasher As PasswordHasher,
                   session As ISessionContext, audit As IAuditService, time As TimeProvider)
        _users = users
        _companies = companies
        _hasher = hasher
        _session = session
        _audit = audit
        _time = time
    End Sub

    Public Function Login(userName As String, password As String) As OperationResult(Of UserSession) Implements IAuthService.Login
        If String.IsNullOrWhiteSpace(userName) OrElse String.IsNullOrEmpty(password) Then
            Return OperationResult(Of UserSession).Fail(InvalidCredentials)
        End If
        Dim name = userName.Trim()
        Dim user = _users.FindByUserName(name)
        If user Is Nothing OrElse Not user.IsActive Then
            _hasher.Equalize(password)
            RecordSecurityEvent("LoginFailed", name, "Unknown or inactive user.")
            Return OperationResult(Of UserSession).Fail(InvalidCredentials)
        End If
        If IsLocked(user) Then
            RecordSecurityEvent("LoginBlocked", name, "Account is locked.")
            Return OperationResult(Of UserSession).Fail(LockedMessage)
        End If
        If Not _hasher.Verify(password, user.PasswordHash, user.PasswordSalt, user.Iterations) Then
            _users.RecordFailedLogin(user.UserId, MaxFailedAttempts, LockoutMinutes)
            RecordSecurityEvent("LoginFailed", name, "Wrong password.")
            Return OperationResult(Of UserSession).Fail(InvalidCredentials)
        End If
        Return CompleteLogin(user)
    End Function

    Public Function ChangePassword(userName As String, currentPassword As String, newPassword As String) As OperationResult Implements IAuthService.ChangePassword
        Dim policy = PasswordPolicy.Validate(newPassword)
        If Not policy.Succeeded Then
            Return policy
        End If
        Dim name = If(userName, String.Empty).Trim()
        Dim user = _users.FindByUserName(name)
        If user Is Nothing OrElse Not user.IsActive OrElse String.IsNullOrEmpty(currentPassword) Then
            _hasher.Equalize(If(currentPassword, String.Empty))
            Return OperationResult.Fail(WrongCurrentPassword)
        End If
        If IsLocked(user) Then
            Return OperationResult.Fail(LockedMessage)
        End If
        If Not _hasher.Verify(currentPassword, user.PasswordHash, user.PasswordSalt, user.Iterations) Then
            _users.RecordFailedLogin(user.UserId, MaxFailedAttempts, LockoutMinutes)
            RecordSecurityEvent("PasswordChangeFailed", name, "Wrong current password.")
            Return OperationResult.Fail(WrongCurrentPassword)
        End If
        If String.Equals(currentPassword, newPassword, StringComparison.Ordinal) Then
            Return OperationResult.Fail("The new password must be different from the current one.")
        End If
        Dim created = _hasher.Create(newPassword)
        _users.SetPassword(user.UserId, created.Hash, created.Salt, created.Iterations, False)
        ClearMustChangeInSession(user.UserId)
        RecordSecurityEvent("PasswordChanged", user.UserName, String.Empty)
        Return OperationResult.Ok()
    End Function

    Private Function CompleteLogin(user As UserRecord) As OperationResult(Of UserSession)
        Dim companyId = 0
        Dim plan = PlanTier.Platinum
        If user.Role <> UserRole.Developer Then
            If Not user.CompanyId.HasValue Then
                Return OperationResult(Of UserSession).Fail("This account is not assigned to a company.")
            End If
            Dim licensed = _companies.GetPlan(user.CompanyId.Value)
            If Not licensed.HasValue Then
                Return OperationResult(Of UserSession).Fail("This company is inactive or has no licence.")
            End If
            companyId = user.CompanyId.Value
            plan = licensed.Value
        End If
        _users.RecordSuccessfulLogin(user.UserId)
        Dim session = New UserSession(user.UserId, user.UserName, user.Role, companyId, plan, user.MustChangePassword)
        _session.SignIn(session)
        RecordSecurityEvent("LoginSucceeded", user.UserName, String.Empty)
        Return OperationResult(Of UserSession).Ok(session)
    End Function

    Private Function IsLocked(user As UserRecord) As Boolean
        Return user.LockedUntilUtc.HasValue AndAlso user.LockedUntilUtc.Value > _time.GetUtcNow().UtcDateTime
    End Function

    Private Sub ClearMustChangeInSession(userId As Integer)
        Dim current = _session.Current
        If current Is Nothing OrElse current.UserId <> userId Then
            Return
        End If
        _session.SignIn(New UserSession(current.UserId, current.UserName, current.Role, current.CompanyId, current.Plan, False))
    End Sub

    Private Sub RecordSecurityEvent(action As String, userName As String, details As String)
        _audit.Record(New AuditEntry(ModuleKey.Security, action, "User", userName, details))
    End Sub
End Class
