''' <summary>Immutable snapshot of the signed-in user. Built by IAuthService, held by ISessionContext.</summary>
Public NotInheritable Class UserSession
    Public Sub New(userId As Integer, userName As String, role As UserRole, companyId As Integer, plan As PlanTier)
        If String.IsNullOrWhiteSpace(userName) Then
            Throw New ArgumentException("A session needs a user name.", NameOf(userName))
        End If
        Me.UserId = userId
        Me.UserName = userName
        Me.Role = role
        Me.CompanyId = companyId
        Me.Plan = plan
    End Sub

    Public ReadOnly Property UserId As Integer
    Public ReadOnly Property UserName As String
    Public ReadOnly Property Role As UserRole
    Public ReadOnly Property CompanyId As Integer
    Public ReadOnly Property Plan As PlanTier
End Class
