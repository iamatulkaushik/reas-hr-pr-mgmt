''' <summary>Creates a company and its Owner login from the running app. The database checks that the caller is an Associate (ADR 0013).</summary>
Public Interface IAssociateAdminRepository
    ''' <summary>Creates a company on the Copper plan and links the calling Associate to it. Returns the company id.</summary>
    Function CreateCompany(companyName As String) As OperationResult(Of Integer)
    ''' <summary>Creates the Owner login of a company the calling Associate works on. The user must change the password at first login.</summary>
    Function CreateOwner(companyId As Integer, userName As String, passwordHash As Byte(), passwordSalt As Byte(), iterations As Integer) As OperationResult(Of Integer)
End Interface
