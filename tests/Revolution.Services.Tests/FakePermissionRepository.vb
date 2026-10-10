Imports Revolution.Core

Public NotInheritable Class FakePermissionRepository
    Implements IPermissionRepository

    Private ReadOnly _grants As New Dictionary(Of UserRole, List(Of PermissionGrant))()

    Public Property LoadCount As Integer

    Public Sub Grant(role As UserRole, moduleKey As ModuleKey, action As PermissionAction)
        If Not _grants.ContainsKey(role) Then
            _grants(role) = New List(Of PermissionGrant)()
        End If
        _grants(role).Add(New PermissionGrant(moduleKey, action))
    End Sub

    Public Function ListForRole(role As UserRole) As IReadOnlyList(Of PermissionGrant) Implements IPermissionRepository.ListForRole
        LoadCount += 1
        Dim found As List(Of PermissionGrant) = Nothing
        If _grants.TryGetValue(role, found) Then
            Return found
        End If
        Return New List(Of PermissionGrant)()
    End Function
End Class
