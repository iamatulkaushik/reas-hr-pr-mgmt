Imports System.Windows.Forms
Imports Revolution.Modules.Attendance

''' <summary>The Attendance module's menu items for the shell (ADR 0016).</summary>
Public NotInheritable Class AttendanceShellModule
    Implements IShellModule

    Private Const AttendanceGroup As String = "Attendance"
    Private Const DailySort As Integer = 10
    Private Const MultipleSort As Integer = 15
    Private Const BulkMultipleSort As Integer = 16
    Private Const BulkImportSort As Integer = 17
    Private Const DeleteSort As Integer = 18
    Private Const MonthViewSort As Integer = 19
    Private Const MonthCloseSort As Integer = 20
    Private Const ShiftSort As Integer = 50
    Private Const HolidaySort As Integer = 60
    Private Const WeeklyOffSort As Integer = 70
    Private Const AssignSort As Integer = 80

    Private ReadOnly _items As IReadOnlyList(Of ShellItem)

    Public Sub New(services As AttendanceServices, permissions As IPermissionService, importer As ITableImporter, exporter As ITableExporter, session As ISessionContext)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If importer Is Nothing Then Throw New ArgumentNullException(NameOf(importer))
        If exporter Is Nothing Then Throw New ArgumentNullException(NameOf(exporter))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        _items = New List(Of ShellItem) From {
            New ShellItem(New MenuEntry("attendance.daily", AttendanceGroup, "Daily attendance", ModuleKey.Attendance, PlanFeature.Attendance, DailySort),
                          Function() CType(New DailyAttendanceForm(services, permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Edit)), Form)),
            New ShellItem(New MenuEntry("attendance.multiple", AttendanceGroup, "Multiple attendance", ModuleKey.Attendance, PlanFeature.Attendance, MultipleSort),
                          Function() CType(New MultipleAttendanceForm(services, permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Edit)), Form)),
            New ShellItem(New MenuEntry("attendance.bulk.multiple", AttendanceGroup, "Multiple attendance (list)", ModuleKey.Attendance, PlanFeature.Attendance, BulkMultipleSort),
                          Function() CType(New BulkEntryForm("Multiple attendance", AttendanceBoardService.BulkTemplate(), services.Board.CreateBulkHandler(), importer, exporter, False, UserNameOf(session)), Form)),
            New ShellItem(New MenuEntry("attendance.bulk.import", AttendanceGroup, "Import attendance", ModuleKey.Attendance, PlanFeature.Attendance, BulkImportSort),
                          Function() CType(New BulkEntryForm("Import attendance", AttendanceBoardService.BulkTemplate(), services.Board.CreateBulkHandler(), importer, exporter, True, UserNameOf(session)), Form)),
            New ShellItem(New MenuEntry("attendance.deleteday", AttendanceGroup, "Delete attendance", ModuleKey.Attendance, PlanFeature.Attendance, DeleteSort),
                          Function() CType(New DeleteAttendanceForm(services, permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Delete)), Form)),
            New ShellItem(New MenuEntry("attendance.monthview", AttendanceGroup, "Month attendance", ModuleKey.Attendance, PlanFeature.Attendance, MonthViewSort),
                          Function() CType(New MonthAttendanceForm(services, permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Edit)), Form)),
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

    Private Shared Function UserNameOf(session As ISessionContext) As String
        Return If(session.Current Is Nothing, String.Empty, session.Current.UserName)
    End Function

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
