''' <summary>Login and password handling. Lockout and forced password change arrive in Phase 2.</summary>
Public Interface IAuthService
    Function Login(userName As String, password As String) As OperationResult(Of UserSession)
    Function ChangePassword(userName As String, currentPassword As String, newPassword As String) As OperationResult
End Interface
