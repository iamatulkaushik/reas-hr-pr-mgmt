Imports System.Windows.Forms
Imports Revolution.Modules.Attendance

''' <summary>The Attendance module's menu items for the shell (ADR 0016).</summary>
Public NotInheritable Class AttendanceShellModule
    Implements IShellModule

    Private Const AttendanceGroup As String = "Attendance"
    Private Const DailySort As Integer = 10
    Private Const MonthCloseSort As Integer = 20
    Private Const ShiftSort As Integer = 50
    Private Const HolidaySort As Integer = 60
    Private Const WeeklyOffSort As Integer = 70
    Private Const AssignSort As Integer = 80

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
                                                              permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Reopen)), Form)),
            New ShellItem(RosterEntry("attendance.roster.shifts", "Shifts", ShiftSort),
                          Function() CType(New ShiftForm(services, CanEditRoster(permissions)), Form)),
            New ShellItem(RosterEntry("attendance.roster.holidays", "Holidays", HolidaySort),
                          Function() CType(New HolidayForm(services, CanEditRoster(permissions)), Form)),
            New ShellItem(RosterEntry("attendance.roster.weeklyoff", "Weekly off", WeeklyOffSort),
                          Function() CType(New WeeklyOffForm(services, CanEditRoster(permissions)), Form)),
            New ShellItem(RosterEntry("attendance.roster.assign", "Shift assignment", AssignSort),
                          Function() CType(New ShiftAssignForm(services, CanEditRoster(permissions)), Form))}
    End Sub

    Private Shared Function RosterEntry(key As String, title As String, sortOrder As Integer) As MenuEntry
        Return New MenuEntry(key, AttendanceGroup, title, ModuleKey.Attendance, PlanFeature.Attendance, sortOrder)
    End Function

    ''' <summary>Only enables or disables the buttons. The service checks Edit again.</summary>
    Private Shared Function CanEditRoster(permissions As IPermissionService) As Boolean
        Return permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Edit)
    End Function

    Public ReadOnly Property Items As IReadOnlyList(Of ShellItem) Implements IShellModule.Items
        Get
            Return _items
        End Get
    End Property
End Class
