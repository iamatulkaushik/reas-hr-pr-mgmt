''' <summary>Answers role-permission questions. Grants are loaded once per role and cached.</summary>
Public NotInheritable Class PermissionService
    Implements IPermissionService

    Private ReadOnly _session As ISessionContext
    Private ReadOnly _repository As IPermissionRepository
    Private ReadOnly _cache As New Dictionary(Of UserRole, HashSet(Of Integer))()

    Public Sub New(session As ISessionContext, repository As IPermissionRepository)
        _session = session
        _repository = repository
    End Sub

    Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
        Dim user = _session.Current
        If user Is Nothing OrElse user.MustChangePassword Then
            Return False
        End If
        Return GrantsFor(user.Role).Contains(KeyOf(moduleKey, action))
    End Function

    ''' <summary>Forget cached grants, for example after the permission matrix is changed.</summary>
    Public Sub InvalidateCache()
        _cache.Clear()
    End Sub

    Private Function GrantsFor(role As UserRole) As HashSet(Of Integer)
        Dim grants As HashSet(Of Integer) = Nothing
        If _cache.TryGetValue(role, grants) Then
            Return grants
        End If
        grants = New HashSet(Of Integer)()
        For Each g In _repository.ListForRole(role)
            grants.Add(KeyOf(g.[Module], g.Action))
        Next
        _cache(role) = grants
        Return grants
    End Function

    Private Shared Function KeyOf(moduleKey As ModuleKey, action As PermissionAction) As Integer
        Return CInt(moduleKey) * 100 + CInt(action)
    End Function
End Class
