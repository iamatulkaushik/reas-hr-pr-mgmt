''' <summary>Decides which menu entries the signed-in user sees. Asks the permission and entitlement services, never a role or plan name.</summary>
Public NotInheritable Class MenuFilter
    Private Sub New()
    End Sub

    Public Shared Function Visible(entries As IEnumerable(Of MenuEntry), permissions As IPermissionService,
                                   entitlements As IEntitlementService) As IReadOnlyList(Of MenuEntry)
        If entries Is Nothing Then Throw New ArgumentNullException(NameOf(entries))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        Dim allowed = entries.Where(Function(entry) IsVisible(entry, permissions, entitlements))
        Dim ordered = allowed.OrderBy(Function(entry) entry.SortOrder).ThenBy(Function(entry) entry.Title, StringComparer.Ordinal)
        Return ordered.ToList()
    End Function

    Private Shared Function IsVisible(entry As MenuEntry, permissions As IPermissionService,
                                      entitlements As IEntitlementService) As Boolean
        If Not permissions.IsAllowed(entry.ModuleKey, PermissionAction.View) Then
            Return False
        End If
        Return Not entry.RequiredFeature.HasValue OrElse entitlements.IsEnabled(entry.RequiredFeature.Value)
    End Function
End Class
