Imports Revolution.Core
Imports Xunit

Public Class MenuUnavailableReasonTests
    Private NotInheritable Class Permissions
        Implements IPermissionService

        Private ReadOnly _viewable As HashSet(Of ModuleKey)

        Public Sub New(ParamArray viewable() As ModuleKey)
            _viewable = New HashSet(Of ModuleKey)(viewable)
        End Sub

        Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
            Return action = PermissionAction.View AndAlso _viewable.Contains(moduleKey)
        End Function
    End Class

    Private NotInheritable Class Entitlements
        Implements IEntitlementService

        Private ReadOnly _enabled As HashSet(Of PlanFeature)

        Public Sub New(ParamArray enabled() As PlanFeature)
            _enabled = New HashSet(Of PlanFeature)(enabled)
        End Sub

        Public ReadOnly Property CurrentPlan As PlanTier Implements IEntitlementService.CurrentPlan
            Get
                Return PlanTier.Copper
            End Get
        End Property

        Public Function IsEnabled(feature As PlanFeature) As Boolean Implements IEntitlementService.IsEnabled
            Return _enabled.Contains(feature)
        End Function
    End Class

    Private Shared Function Entry(key As String, moduleKey As ModuleKey, feature As PlanFeature?, sort As Integer) As MenuEntry
        Return New MenuEntry(key, "Group", key, moduleKey, feature, sort)
    End Function

    <Fact>
    Public Sub OpenableEntry_HasNoReason()
        Dim reason As String = MenuFilter.UnavailableReason(Entry("a", ModuleKey.Attendance, PlanFeature.Attendance, 1), New Permissions(ModuleKey.Attendance), New Entitlements(PlanFeature.Attendance))
        Assert.Null(reason)
    End Sub

    <Fact>
    Public Sub NoViewPermission_SaysRoleOrLicence()
        Dim reason As String = MenuFilter.UnavailableReason(Entry("a", ModuleKey.Attendance, PlanFeature.Attendance, 1), New Permissions(ModuleKey.Masters), New Entitlements(PlanFeature.Attendance))
        Assert.Equal("Not available for your role, or not in your licence.", reason)
    End Sub

    <Fact>
    Public Sub FeatureOffPlan_NamesTheTier()
        Dim reason As String = MenuFilter.UnavailableReason(Entry("t", ModuleKey.Payroll, PlanFeature.Tds, 1), New Permissions(ModuleKey.Payroll), New Entitlements())
        Assert.Equal("Needs the Gold plan or higher.", reason)
    End Sub

    <Fact>
    Public Sub Ordered_KeepsEveryEntry_BySortOrderThenTitle()
        Dim all As IReadOnlyList(Of MenuEntry) = MenuFilter.Ordered({Entry("b", ModuleKey.Masters, Nothing, 20), Entry("a", ModuleKey.Masters, Nothing, 20), Entry("c", ModuleKey.Masters, Nothing, 10)})
        Assert.Equal("c", all(0).EntryKey)
        Assert.Equal("a", all(1).EntryKey)
        Assert.Equal("b", all(2).EntryKey)
    End Sub

    <Fact>
    Public Sub NothingPassed_Throws()
        Assert.Throws(Of ArgumentNullException)(Function() MenuFilter.UnavailableReason(Nothing, New Permissions(), New Entitlements()))
        Assert.Throws(Of ArgumentNullException)(Function() MenuFilter.Ordered(Nothing))
    End Sub
End Class
