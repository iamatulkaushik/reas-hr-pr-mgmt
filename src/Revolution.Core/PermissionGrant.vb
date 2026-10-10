''' <summary>One allowed action on one module for a role.</summary>
Public NotInheritable Class PermissionGrant
    Public Sub New(moduleKey As ModuleKey, action As PermissionAction)
        Me.[Module] = moduleKey
        Me.Action = action
    End Sub

    Public ReadOnly Property [Module] As ModuleKey
    Public ReadOnly Property Action As PermissionAction
End Class
