Imports Revolution.Core

Public NotInheritable Class FakeProvisioningRepository
    Implements IProvisioningRepository

    Public Property CreatedUsers As Integer
    Public Property LastMustChange As Boolean
    Public Property LastHash As Byte()
    Public Property LastSalt As Byte()
    Public Property LastIterations As Integer

    Public Function CreateCompany(companyName As String, plan As PlanTier) As Integer Implements IProvisioningRepository.CreateCompany
        Return 1
    End Function

    Public Function CreateUser(userName As String, companyId As Integer?, role As UserRole, passwordHash As Byte(),
                               passwordSalt As Byte(), iterations As Integer,
                               mustChangePassword As Boolean) As Integer Implements IProvisioningRepository.CreateUser
        CreatedUsers += 1
        LastMustChange = mustChangePassword
        LastHash = passwordHash
        LastSalt = passwordSalt
        LastIterations = iterations
        Return 100 + CreatedUsers
    End Function
End Class
