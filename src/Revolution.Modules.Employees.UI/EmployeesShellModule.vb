Imports System.Windows.Forms
Imports Revolution.Modules.Employees

''' <summary>The Employees module's menu items for the shell (ADR 0016). Employee screens are added here as they ship.</summary>
Public NotInheritable Class EmployeesShellModule
    Implements IShellModule

    Private Const MastersGroup As String = "Masters"

    Private ReadOnly _services As EmployeesServices
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _items As IReadOnlyList(Of ShellItem)

    Public Sub New(services As EmployeesServices, permissions As IPermissionService)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        _services = services
        _permissions = permissions
        _items = BuildItems()
    End Sub

    Public ReadOnly Property Items As IReadOnlyList(Of ShellItem) Implements IShellModule.Items
        Get
            Return _items
        End Get
    End Property

    Private Function BuildItems() As IReadOnlyList(Of ShellItem)
        Return New List(Of ShellItem) From {
            New ShellItem(MasterEntry("masters.employee", "Employee", 5), Function() CType(New EmployeeForm(_services, CanEditEmployees()), Form)),
            New ShellItem(MasterEntry("masters.factory", "Factory", 10), Function() CType(New FactoryForm(_services, CanEditMasters()), Form)),
            New ShellItem(MasterEntry("masters.department", "Department", 20), Function() CType(New DepartmentForm(_services, CanEditMasters()), Form)),
            New ShellItem(MasterEntry("masters.designation", "Designation", 30), Function() CType(New DesignationForm(_services, CanEditMasters()), Form))}
    End Function

    Private Shared Function MasterEntry(key As String, title As String, sortOrder As Integer) As MenuEntry
        Return New MenuEntry(key, MastersGroup, title, ModuleKey.Masters, PlanFeature.Employees, sortOrder)
    End Function

    Private Function CanEditEmployees() As Boolean
        Return _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.Edit) OrElse
               _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.Create)
    End Function

    ''' <summary>Create and Edit are checked again in the service. This only enables or disables the buttons.</summary>
    Private Function CanEditMasters() As Boolean
        Return _permissions.IsAllowed(ModuleKey.Masters, PermissionAction.Edit) OrElse
               _permissions.IsAllowed(ModuleKey.Masters, PermissionAction.Create)
    End Function
End Class
