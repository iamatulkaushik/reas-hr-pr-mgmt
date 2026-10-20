Imports Revolution.UI.Common

''' <summary>
''' The application menu layout (Sigma). Feature modules offer screens under their own keys; this class decides where each one
''' sits in the menu and lists the positions agreed with the product owner whose screens do not exist yet (shown disabled).
''' To add a screen: change one line from Planned to Screen. Modules themselves are not registered in Program.vb, only this class.
''' </summary>
Public NotInheritable Class MenuLayoutModule
    Implements IShellModule

    Private Const MasterGroup As String = "Master"
    Private Const CompanyGroup As String = "Master|Company"
    Private Const UnitsGroup As String = "Master|Company|Factory / Units"
    Private Const EmployeeGroup As String = "Employee"
    Private Const AddEmployeeGroup As String = "Employee|Add Employee"
    Private Const UpdateEmployeeGroup As String = "Employee|Update"
    Private Const AttendanceGroup As String = "Attendance"
    Private Const InsertAttendanceGroup As String = "Attendance|Insert"

    ' Entry keys of the screens the feature modules offer.
    Private Const EmployeeScreen As String = "masters.employee"
    Private Const FactoryScreen As String = "masters.factory"
    Private Const DepartmentScreen As String = "masters.department"
    Private Const DesignationScreen As String = "masters.designation"
    Private Const DailyAttendanceScreen As String = "attendance.daily"
    Private Const MonthCloseScreen As String = "attendance.monthclose"

    Private ReadOnly _screens As Dictionary(Of String, ShellItem)
    Private ReadOnly _items As New List(Of ShellItem)()

    ''' <param name="sources">The feature modules whose screens may be placed in the menu.</param>
    Public Sub New(sources As IEnumerable(Of IShellModule))
        If sources Is Nothing Then Throw New ArgumentNullException(NameOf(sources))
        _screens = sources.SelectMany(Function(m) m.Items).ToDictionary(Function(item) item.Entry.EntryKey, StringComparer.Ordinal)
        AddMaster()
        AddEmployee()
        AddAttendance()
    End Sub

    Public ReadOnly Property Items As IReadOnlyList(Of ShellItem) Implements IShellModule.Items
        Get
            Return _items
        End Get
    End Property

    Private Sub AddMaster()
        AddPlanned("master.company.basic", CompanyGroup, "Basic", ModuleKey.Masters, Nothing, 100)
        AddPlanned("master.company.statutory", CompanyGroup, "Statutory / Banking", ModuleKey.Masters, Nothing, 110)
        AddScreen("master.company.units", UnitsGroup, "Units", ModuleKey.Masters, Nothing, 120, FactoryScreen)
        AddPlanned("master.company.shifts", UnitsGroup, "Shifts", ModuleKey.Masters, Nothing, 130)
        AddPlanned("master.company.shut", CompanyGroup, "Shut", ModuleKey.Masters, Nothing, 140)
        AddScreen("master.department", MasterGroup, "Department", ModuleKey.Masters, Nothing, 150, DepartmentScreen)
        AddScreen("master.designation", MasterGroup, "Designation", ModuleKey.Masters, Nothing, 160, DesignationScreen)
        AddPlanned("master.dailywages", MasterGroup, "Daily Wages", ModuleKey.Masters, Nothing, 170)
        AddPlanned("master.holidays", MasterGroup, "Holidays", ModuleKey.Masters, Nothing, 180)
    End Sub

    Private Sub AddEmployee()
        Dim feature As PlanFeature? = PlanFeature.Employees
        AddScreen("employee.add.single", AddEmployeeGroup, "Single Employee", ModuleKey.Employees, feature, 200, EmployeeScreen)
        AddPlanned("employee.add.multiple", AddEmployeeGroup, "Multiple Employee", ModuleKey.Employees, feature, 210)
        AddPlanned("employee.add.import", AddEmployeeGroup, "Bulk Import Employee", ModuleKey.Employees, feature, 220)
        ' One Employee screen holds all the tabs, so these open it until separate screens exist.
        AddScreen("employee.update.basic", UpdateEmployeeGroup, "Basic", ModuleKey.Employees, feature, 230, EmployeeScreen)
        AddScreen("employee.update.employment", UpdateEmployeeGroup, "Employment", ModuleKey.Employees, feature, 240, EmployeeScreen)
        AddScreen("employee.update.statutory", UpdateEmployeeGroup, "Statutory", ModuleKey.Employees, feature, 250, EmployeeScreen)
        AddScreen("employee.update.nomination", UpdateEmployeeGroup, "Nomination", ModuleKey.Employees, feature, 260, EmployeeScreen)
        AddPlanned("employee.update.education", UpdateEmployeeGroup, "Education", ModuleKey.Employees, feature, 270)
        AddPlanned("employee.retire", EmployeeGroup, "Disable / Retire", ModuleKey.Employees, feature, 280)
        AddPlanned("employee.delete", EmployeeGroup, "Delete", ModuleKey.Employees, feature, 290)
    End Sub

    Private Sub AddAttendance()
        Dim feature As PlanFeature? = PlanFeature.Attendance
        AddScreen("attendance.insert.single", InsertAttendanceGroup, "Single", ModuleKey.Attendance, feature, 300, DailyAttendanceScreen)
        AddPlanned("attendance.insert.multiple", InsertAttendanceGroup, "Multiple", ModuleKey.Attendance, feature, 310)
        AddPlanned("attendance.insert.import", InsertAttendanceGroup, "Import", ModuleKey.Attendance, feature, 320)
        AddScreen("attendance.update", AttendanceGroup, "Update", ModuleKey.Attendance, feature, 330, DailyAttendanceScreen)
        ' Not in the agreed list, kept because payroll needs the month lock.
        AddScreen("attendance.monthclose", AttendanceGroup, "Month close", ModuleKey.Attendance, feature, 340, MonthCloseScreen)
    End Sub

    ''' <summary>A position with a screen. When the module that offers the screen is not registered the position is shown as planned.</summary>
    Private Sub AddScreen(key As String, group As String, title As String, moduleKey As ModuleKey, feature As PlanFeature?,
                       sortOrder As Integer, screenKey As String)
        Dim entry As New MenuEntry(key, group, title, moduleKey, feature, sortOrder)
        Dim source As ShellItem = Nothing
        If _screens.TryGetValue(screenKey, source) Then
            _items.Add(New ShellItem(entry, source.OpenScreen))
        Else
            _items.Add(ShellItem.Planned(entry))
        End If
    End Sub

    Private Sub AddPlanned(key As String, group As String, title As String, moduleKey As ModuleKey, feature As PlanFeature?, sortOrder As Integer)
        _items.Add(ShellItem.Planned(New MenuEntry(key, group, title, moduleKey, feature, sortOrder)))
    End Sub
End Class
