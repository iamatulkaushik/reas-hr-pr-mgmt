''' <summary>Role permissions. Screens call this; they never compare role names.</summary>
Public Interface IPermissionService
    Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean
End Interface
