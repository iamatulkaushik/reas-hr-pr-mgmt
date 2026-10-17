Imports Revolution.Core

''' <summary>Reference lists the employee screens need (banks). Needs View on Employees.</summary>
Public NotInheritable Class ReferenceService
    Private ReadOnly _repository As IOrgMasterRepository
    Private ReadOnly _permissions As IPermissionService

    Public Sub New(repository As IOrgMasterRepository, permissions As IPermissionService)
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        _repository = repository
        _permissions = permissions
    End Sub

    Public Function ListBanks() As OperationResult(Of IReadOnlyList(Of BankItem))
        If Not _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.View) Then
            Return OperationResult(Of IReadOnlyList(Of BankItem)).Fail("You are not allowed to do this.")
        End If
        Return OperationResult(Of IReadOnlyList(Of BankItem)).Ok(_repository.ListBanks())
    End Function
End Class
