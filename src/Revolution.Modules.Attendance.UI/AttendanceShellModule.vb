Imports System.Windows.Forms
Imports Revolution.Modules.Attendance

''' <summary>The Attendance module's menu items for the shell (ADR 0016).</summary>
Public NotInheritable Class AttendanceShellModule
    Implements IShellModule

    Private Const AttendanceGroup As String = "Attendance"
    Private Const DailySort As Integer = 10
    Private Const MonthCloseSort As Integer = 20

    Private ReadOnly _items As IReadOnlyList(Of ShellItem)

    Public Sub New(services As AttendanceServices, permissions As IPermissionService)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        _items = New List(Of ShellItem) From {
            New ShellItem(New MenuEntry("attendance.daily", AttendanceGroup, "Daily attendance", ModuleKey.Attendance, PlanFeature.Attendance, DailySort),
                          Function() CType(New DailyAttendanceForm(services, permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Edit)), Form)),
            New ShellItem(New MenuEntry("attendance.monthclose", AttendanceGroup, "Month close", ModuleKey.Attendance, PlanFeature.Attendance, MonthCloseSort),
                          Function() CType(New MonthCloseForm(services,
                                                              permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Approve),
                                                              permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Reopen)), Form))}
    End Sub

    Public ReadOnly Property Items As IReadOnlyList(Of ShellItem) Implements IShellModule.Items
        Get
            Return _items
        End Get
    End Property
End Class
