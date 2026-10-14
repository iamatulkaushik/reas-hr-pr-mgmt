''' <summary>Login and password handling. Lockout and forced password change arrive in Phase 2.</summary>
Public Interface IAuthService
    Function Login(userName As String, password As String) As OperationResult(Of UserSession)
    ''' <summary>The companies the signed-in Associate works on. Fails when nobody is signed in or the user is not an Associate.</summary>
    Function ListCompanies() As OperationResult(Of IReadOnlyList(Of CompanySummary))
    ''' <summary>Opens one of those companies: the session company and plan change, so row-level security follows.</summary>
    Function SelectCompany(companyId As Integer) As OperationResult(Of UserSession)
    Function ChangePassword(userName As String, currentPassword As String, newPassword As String) As OperationResult
End Interface
