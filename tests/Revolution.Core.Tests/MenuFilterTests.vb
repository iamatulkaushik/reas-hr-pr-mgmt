Imports Revolution.Core
Imports Xunit

Public Class MenuFilterTests
    Private NotInheritable Class FakePermissions
        Implements IPermissionService

        Private ReadOnly _viewable As HashSet(Of ModuleKey)

        Public Sub New(ParamArray viewable() As ModuleKey)
            _viewable = New HashSet(Of ModuleKey)(viewable)
        End Sub

        Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
            Return action = PermissionAction.View AndAlso _viewable.Contains(moduleKey)
        End Function
    End Class

    Private NotInheritable Class FakeEntitlements
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

    <Fact>
    Public Sub Hides_EntriesTheUserCannotView()
        Dim entries = {
            New MenuEntry("emp.list", "Employees", "Employee list", ModuleKey.Employees),
            New MenuEntry("pay.runs", "Payroll", "Payroll runs", ModuleKey.Payroll)
        }
        Dim visible = MenuFilter.Visible(entries, New FakePermissions(ModuleKey.Employees), New FakeEntitlements())
        Assert.Equal({"emp.list"}, visible.Select(Function(entry) entry.EntryKey).ToArray())
    End Sub

    <Fact>
    Public Sub Hides_EntriesWhosePlanFeatureIsOff_AndShowsWhenNoFeatureIsNeeded()
        Dim entries = {
            New MenuEntry("pay.loans", "Payroll", "Loans", ModuleKey.Payroll, PlanFeature.Loans),
            New MenuEntry("pay.runs", "Payroll", "Payroll runs", ModuleKey.Payroll)
        }
        Dim visible = MenuFilter.Visible(entries, New FakePermissions(ModuleKey.Payroll), New FakeEntitlements())
        Assert.Equal({"pay.runs"}, visible.Select(Function(entry) entry.EntryKey).ToArray())
        Dim withLoans = MenuFilter.Visible(entries, New FakePermissions(ModuleKey.Payroll), New FakeEntitlements(PlanFeature.Loans))
        Assert.Equal(2, withLoans.Count)
    End Sub

    <Fact>
    Public Sub Orders_BySortOrderThenTitle()
        Dim entries = {
            New MenuEntry("b", "Payroll", "Zeta", ModuleKey.Payroll, sortOrder:=10),
            New MenuEntry("c", "Payroll", "Alpha", ModuleKey.Payroll, sortOrder:=20),
            New MenuEntry("a", "Payroll", "Beta", ModuleKey.Payroll, sortOrder:=10)
        }
        Dim visible = MenuFilter.Visible(entries, New FakePermissions(ModuleKey.Payroll), New FakeEntitlements())
        Assert.Equal({"a", "b", "c"}, visible.Select(Function(entry) entry.EntryKey).ToArray())
    End Sub

    <Fact>
    Public Sub Entry_RequiresKeyGroupAndTitle()
        Assert.Throws(Of ArgumentException)(Function() New MenuEntry(" ", "Payroll", "Runs", ModuleKey.Payroll))
        Assert.Throws(Of ArgumentException)(Function() New MenuEntry("k", " ", "Runs", ModuleKey.Payroll))
        Assert.Throws(Of ArgumentException)(Function() New MenuEntry("k", "Payroll", " ", ModuleKey.Payroll))
    End Sub
End Class
