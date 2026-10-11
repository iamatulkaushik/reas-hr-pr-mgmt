''' <summary>Creates companies and users. Used by the setup tool, never by the running app (its login is denied these procedures).</summary>
Public Interface IProvisioningRepository
    Function CreateCompany(companyName As String, plan As PlanTier) As Integer
    Function CreateUser(userName As String, companyId As Integer?, role As UserRole, passwordHash As Byte(),
                        passwordSalt As Byte(), iterations As Integer, mustChangePassword As Boolean) As Integer
End Interface
