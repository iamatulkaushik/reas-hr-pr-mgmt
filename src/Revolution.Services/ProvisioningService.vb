''' <summary>Creates companies and users with validated input. New users must change their password at first login.</summary>
Public NotInheritable Class ProvisioningService
    Private Const MaxUserNameLength As Integer = 100

    Private ReadOnly _users As IUserRepository
    Private ReadOnly _companies As ICompanyRepository
    Private ReadOnly _provisioning As IProvisioningRepository
    Private ReadOnly _hasher As PasswordHasher

    Public Sub New(users As IUserRepository, companies As ICompanyRepository,
                   provisioning As IProvisioningRepository, hasher As PasswordHasher)
        _users = users
        _companies = companies
        _provisioning = provisioning
        _hasher = hasher
    End Sub

    Public Function CreateCompany(companyName As String, plan As PlanTier) As OperationResult(Of Integer)
        If String.IsNullOrWhiteSpace(companyName) Then
            Return OperationResult(Of Integer).Fail("A company needs a name.")
        End If
        If Not [Enum].IsDefined(GetType(PlanTier), plan) Then
            Return OperationResult(Of Integer).Fail("Unknown plan.")
        End If
        Return OperationResult(Of Integer).Ok(_provisioning.CreateCompany(companyName.Trim(), plan))
    End Function

    Public Function CreateUser(userName As String, password As String, role As UserRole,
                               companyId As Integer?) As OperationResult(Of Integer)
        Dim problem = Validate(userName, password, role, companyId)
        If problem IsNot Nothing Then
            Return OperationResult(Of Integer).Fail(problem)
        End If
        Dim name = userName.Trim()
        If _users.FindByUserName(name) IsNot Nothing Then
            Return OperationResult(Of Integer).Fail("That user name is already taken.")
        End If
        Dim created = _hasher.Create(password)
        Return OperationResult(Of Integer).Ok(_provisioning.CreateUser(name, companyId, role, created.Hash,
                                                                        created.Salt, created.Iterations, True))
    End Function

    ''' <summary>Returns a message when the input is not acceptable, otherwise Nothing.</summary>
    Private Function Validate(userName As String, password As String, role As UserRole, companyId As Integer?) As String
        If String.IsNullOrWhiteSpace(userName) OrElse userName.Trim().Length > MaxUserNameLength Then
            Return $"A user name is required, up to {MaxUserNameLength} characters."
        End If
        If Not [Enum].IsDefined(GetType(UserRole), role) Then
            Return "Unknown role."
        End If
        If role = UserRole.Developer AndAlso companyId.HasValue Then
            Return "A Developer belongs to no company."
        End If
        If role <> UserRole.Developer Then
            If Not companyId.HasValue Then
                Return "This role needs a company."
            End If
            If Not _companies.GetPlan(companyId.Value).HasValue Then
                Return "Company not found or inactive."
            End If
        End If
        Dim policy = PasswordPolicy.Validate(password)
        Return If(policy.Succeeded, Nothing, policy.Message)
    End Function
End Class
