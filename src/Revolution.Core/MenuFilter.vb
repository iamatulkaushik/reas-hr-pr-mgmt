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

    ''' <summary>Every entry in menu order (sort order, then title), whether or not the user may open it.</summary>
    Public Shared Function Ordered(entries As IEnumerable(Of MenuEntry)) As IReadOnlyList(Of MenuEntry)
        If entries Is Nothing Then Throw New ArgumentNullException(NameOf(entries))
        Return entries.OrderBy(Function(entry) entry.SortOrder).ThenBy(Function(entry) entry.Title, StringComparer.Ordinal).ToList()
    End Function

    ''' <summary>
    ''' Why the signed-in user cannot open an entry, in words for a tooltip, or Nothing when they can. Same two questions as Visible, asked in the same order: the role and licence
    ''' (View permission), then the plan feature. Used to list every entry with the unavailable ones disabled instead of hidden.
    ''' </summary>
    Public Shared Function UnavailableReason(entry As MenuEntry, permissions As IPermissionService, entitlements As IEntitlementService) As String
        If entry Is Nothing Then Throw New ArgumentNullException(NameOf(entry))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If Not permissions.IsAllowed(entry.ModuleKey, PermissionAction.View) Then
            Return "Not available for your role, or not in your licence."
        End If
        If entry.RequiredFeature.HasValue AndAlso Not entitlements.IsEnabled(entry.RequiredFeature.Value) Then
            Return $"Needs the {PlanFeatureMap.MinimumTier(entry.RequiredFeature.Value)} plan or higher."
        End If
        Return Nothing
    End Function

    Private Shared Function IsVisible(entry As MenuEntry, permissions As IPermissionService,
                                      entitlements As IEntitlementService) As Boolean
        If Not permissions.IsAllowed(entry.ModuleKey, PermissionAction.View) Then
            Return False
        End If
        Return Not entry.RequiredFeature.HasValue OrElse entitlements.IsEnabled(entry.RequiredFeature.Value)
    End Function
End Class
