''' <summary>Role permission storage.</summary>
Public Interface IPermissionRepository
    Function ListForRole(role As UserRole) As IReadOnlyList(Of PermissionGrant)
End Interface
