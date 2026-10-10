''' <summary>Immutable snapshot of the signed-in user. Built by IAuthService, held by ISessionContext.</summary>
Public NotInheritable Class UserSession
    Public Sub New(userId As Integer, userName As String, role As UserRole, companyId As Integer, plan As PlanTier,
                   Optional mustChangePassword As Boolean = False)
        If String.IsNullOrWhiteSpace(userName) Then
            Throw New ArgumentException("A session needs a user name.", NameOf(userName))
        End If
        Me.UserId = userId
        Me.UserName = userName
        Me.Role = role
        Me.CompanyId = companyId
        Me.Plan = plan
        Me.MustChangePassword = mustChangePassword
    End Sub

    Public ReadOnly Property UserId As Integer
    Public ReadOnly Property UserName As String
    Public ReadOnly Property Role As UserRole
    ''' <summary>The user's company. 0 for the Developer role, which belongs to no company.</summary>
    Public ReadOnly Property CompanyId As Integer
    Public ReadOnly Property Plan As PlanTier
    ''' <summary>While True, IPermissionService denies everything until the password is changed.</summary>
    Public ReadOnly Property MustChangePassword As Boolean
End Class
