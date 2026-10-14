''' <summary>What an Associate does about companies: create one, create the Owner login for one. Needs Create on Security.</summary>
Public Interface IAssociateAdminService
    Function CreateCompany(companyName As String) As OperationResult(Of Integer)
    Function CreateOwner(companyId As Integer, userName As String, temporaryPassword As String) As OperationResult(Of Integer)
End Interface
