Imports Revolution.Core

Friend NotInheritable Class FakeAssociateAdminRepository
    Implements IAssociateAdminRepository

    Public Property CompaniesCreated As New List(Of String)()
    Public Property OwnersCreated As New List(Of String)()
    Public Property LastOwnerCompanyId As Integer
    Public Property LastHash As Byte()
    Public Property TakenUserName As String = "taken"
    Public Property NextId As Integer = 41

    Public Function CreateCompany(companyName As String) As OperationResult(Of Integer) Implements IAssociateAdminRepository.CreateCompany
        CompaniesCreated.Add(companyName)
        NextId += 1
        Return OperationResult(Of Integer).Ok(NextId)
    End Function

    Public Function CreateOwner(companyId As Integer, userName As String, passwordHash As Byte(), passwordSalt As Byte(),
                                iterations As Integer) As OperationResult(Of Integer) Implements IAssociateAdminRepository.CreateOwner
        If String.Equals(userName, TakenUserName, StringComparison.OrdinalIgnoreCase) Then
            Return OperationResult(Of Integer).Fail("That user name is already taken.")
        End If
        OwnersCreated.Add(userName)
        LastOwnerCompanyId = companyId
        LastHash = passwordHash
        NextId += 1
        Return OperationResult(Of Integer).Ok(NextId)
    End Function
End Class
