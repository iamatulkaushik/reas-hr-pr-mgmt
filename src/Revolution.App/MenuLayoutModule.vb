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
    Private Const CareerGroup As String = "Career"
    Private Const ProcessGroup As String = "Process"
    Private Const MonthlyGroup As String = "Compliance|Monthly"
    Private Const HalfYearlyGroup As String = "Compliance|Half-Yearly"
    Private Const AnnualGroup As String = "Compliance|Annual"
    Private Const ReportsGroup As String = "Reports"
    Private Const DetailedGroup As String = "Reports|Detailed"
    Private Const CaReportsGroup As String = "Reports|CA Reports"

    ' Entry keys of the screens the feature modules offer.
    Private Const EmployeeScreen As String = "masters.employee"
    Private Const FactoryScreen As String = "masters.factory"
    Private Const DepartmentScreen As String = "masters.department"
    Private Const DesignationScreen As String = "masters.designation"
    Private Const DailyAttendanceScreen As String = "attendance.daily"
    Private Const MultipleAttendanceScreen As String = "attendance.multiple"
    Private Const BulkMultipleScreen As String = "attendance.bulk.multiple"
    Private Const BulkImportScreen As String = "attendance.bulk.import"
    Private Const DeleteAttendanceScreen As String = "attendance.deleteday"
    Private Const MonthViewScreen As String = "attendance.monthview"
    Private Const MonthCloseScreen As String = "attendance.monthclose"
    Private Const ShiftScreen As String = "attendance.roster.shifts"
    Private Const HolidayScreen As String = "attendance.roster.holidays"
    Private Const WeeklyOffScreen As String = "attendance.roster.weeklyoff"
    Private Const ShiftAssignScreen As String = "attendance.roster.assign"
    Private Const EcrScreen As String = "payroll.export.ecr"
    Private Const EsicScreen As String = "payroll.export.esic"
    Private Const BankScreen As String = "payroll.export.bank"
    Private Const FnfScreen As String = "payroll.fnf"
    Private Const ArrearsListScreen As String = "arrears.list"
    Private Const ArrearsReportScreen As String = "arrears.report"
    Private Const RunScreen As String = "payroll.run"
    Private Const LockScreen As String = "payroll.lock"
    Private Const BonusScreen As String = "payroll.bonus"
    Private Const PayslipScreen As String = "payroll.payslips"
    Private Const LeaveScreen As String = "payroll.leave"
    Private Const LeaveAccrualScreen As String = "payroll.leaveaccrual"
    Private Const TdsScreen As String = "payroll.tds"
    Private Const LoansScreen As String = "payroll.loans"

    Private ReadOnly _screens As Dictionary(Of String, ShellItem)
    Private ReadOnly _items As New List(Of ShellItem)()
    Private ReadOnly _placedScreens As New HashSet(Of String)(StringComparer.Ordinal)

    ''' <param name="sources">The feature modules whose screens may be placed in the menu.</param>
    Public Sub New(sources As IEnumerable(Of IShellModule))
        If sources Is Nothing Then Throw New ArgumentNullException(NameOf(sources))
        _screens = sources.SelectMany(Function(m) m.Items).ToDictionary(Function(item) item.Entry.EntryKey, StringComparer.Ordinal)
        AddMaster()
        AddEmployee()
        AddAttendance()
        AddCareer()
        AddProcess()
        AddCompliance()
        AddReports()
        RejectScreensWithoutPosition()
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
        ' Shifts and Holidays sit under Master as agreed but belong to the Attendance module, so they follow its permission and plan feature.
        AddScreen("master.company.shifts", UnitsGroup, "Shifts", ModuleKey.Attendance, PlanFeature.Attendance, 130, ShiftScreen)
        AddPlanned("master.company.shut", CompanyGroup, "Shut", ModuleKey.Masters, Nothing, 140)
        AddScreen("master.department", MasterGroup, "Department", ModuleKey.Masters, Nothing, 150, DepartmentScreen)
        AddScreen("master.designation", MasterGroup, "Designation", ModuleKey.Masters, Nothing, 160, DesignationScreen)
        AddPlanned("master.dailywages", MasterGroup, "Daily Wages", ModuleKey.Masters, Nothing, 170)
        AddScreen("master.holidays", MasterGroup, "Holidays", ModuleKey.Attendance, PlanFeature.Attendance, 180, HolidayScreen)
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
        AddScreen("attendance.insert.multiple", InsertAttendanceGroup, "Multiple", ModuleKey.Attendance, feature, 310, BulkMultipleScreen)
        AddScreen("attendance.insert.range", InsertAttendanceGroup, "Range", ModuleKey.Attendance, feature, 315, MultipleAttendanceScreen)
        AddScreen("attendance.insert.import", InsertAttendanceGroup, "Import", ModuleKey.Attendance, feature, 320, BulkImportScreen)
        AddScreen("attendance.update", AttendanceGroup, "Update", ModuleKey.Attendance, feature, 330, DailyAttendanceScreen)
        ' Delete is for the Associate only, and only before the month is locked: the permission and the lock check come with the screen.
        AddScreen("attendance.delete", AttendanceGroup, "Delete", ModuleKey.Attendance, feature, 335, DeleteAttendanceScreen)
        AddScreen("attendance.monthview", AttendanceGroup, "Month view", ModuleKey.Attendance, feature, 337, MonthViewScreen)
        ' Not in the agreed list, kept because payroll needs the month lock.
        AddScreen("attendance.monthclose", AttendanceGroup, "Month close", ModuleKey.Attendance, feature, 340, MonthCloseScreen)
        ' Not in the agreed list either: the screens exist and the roster needs them (weekly off days, and who works which shift).
        AddScreen("attendance.weeklyoff", AttendanceGroup, "Weekly off", ModuleKey.Attendance, feature, 350, WeeklyOffScreen)
        AddScreen("attendance.shiftassign", AttendanceGroup, "Shift assignment", ModuleKey.Attendance, feature, 360, ShiftAssignScreen)
    End Sub

    ''' <summary>Career (agreed 2026-10-31). The screens are not built yet.</summary>
    Private Sub AddCareer()
        Dim feature As PlanFeature? = PlanFeature.Employees
        AddPlanned("career.promotion", CareerGroup, "Promotion", ModuleKey.Employees, feature, 400)
        AddPlanned("career.increment", CareerGroup, "Increment", ModuleKey.Employees, feature, 410)
        AddPlanned("career.transfer", CareerGroup, "Transfer", ModuleKey.Employees, feature, 420)
    End Sub

    ''' <summary>
    ''' Process (agreed 2026-10-31): the monthly run, its lock, bonuses and full and final. Bank file, income tax declaration, loans and
    ''' leave ledger are not in the agreed list; they stay here because their screens exist and payroll needs them.
    ''' </summary>
    Private Sub AddProcess()
        AddScreen("payroll.run", ProcessGroup, "Payroll", ModuleKey.Payroll, PlanFeature.BasicPayroll, 500, RunScreen)
        AddScreen("payroll.lock", ProcessGroup, "Lock Payroll", ModuleKey.Payroll, PlanFeature.BasicPayroll, 510, LockScreen)
        AddScreen("payroll.bonus", ProcessGroup, "Bonuses", ModuleKey.Payroll, PlanFeature.BasicPayroll, 520, BonusScreen)
        AddScreen("payroll.fnf", ProcessGroup, "Full and Final Settlements", ModuleKey.Payroll, PlanFeature.FullAndFinal, 530, FnfScreen)
        AddScreen("payroll.export.bank", ProcessGroup, "Bank file", ModuleKey.Payroll, PlanFeature.BankFiles, 540, BankScreen)
        AddScreen("payroll.tds", ProcessGroup, "Income tax declaration", ModuleKey.Payroll, PlanFeature.Tds, 550, TdsScreen)
        AddScreen("payroll.loans", ProcessGroup, "Loans", ModuleKey.Payroll, PlanFeature.Loans, 560, LoansScreen)
        AddScreen("payroll.leave", ProcessGroup, "Leave ledger", ModuleKey.Payroll, PlanFeature.FullAndFinal, 570, LeaveScreen)
        AddScreen("payroll.leaveaccrual", ProcessGroup, "Leave accrual", ModuleKey.Payroll, PlanFeature.FullAndFinal, 575, LeaveAccrualScreen)
        AddScreen("arrears.list", ProcessGroup, "Arrears", ModuleKey.Payroll, PlanFeature.Arrears, 580, ArrearsListScreen)
    End Sub

    ''' <summary>
    ''' Compliance (agreed 2026-10-31), by how often the filing falls due. The EPFO and ESIC files are statutory filings (ADR 0019).
    ''' The register and return forms are listed as planned; Haryana forms come first.
    ''' </summary>
    Private Sub AddCompliance()
        AddScreen("compliance.monthly.epfo", MonthlyGroup, "EPFO (ECR file)", ModuleKey.Statutory, PlanFeature.EpfEsi, 600, EcrScreen)
        AddScreen("compliance.monthly.esic", MonthlyGroup, "ESIC (upload sheet)", ModuleKey.Statutory, PlanFeature.EpfEsi, 610, EsicScreen)
        AddPlanned("compliance.halfyearly.form20cl", HalfYearlyGroup, "Form 20 CL", ModuleKey.Statutory, Nothing, 620)
        AddPlanned("compliance.halfyearly.lwfchallan", HalfYearlyGroup, "LWF Challan", ModuleKey.Statutory, Nothing, 630)
        AddPlanned("compliance.annual.form34", AnnualGroup, "Form 34", ModuleKey.Statutory, Nothing, 640)
        AddPlanned("compliance.annual.formv", AnnualGroup, "Form V", ModuleKey.Statutory, Nothing, 650)
        AddPlanned("compliance.annual.formiv", AnnualGroup, "Form IV", ModuleKey.Statutory, Nothing, 660)
        AddPlanned("compliance.annual.formd", AnnualGroup, "Form D", ModuleKey.Statutory, Nothing, 670)
    End Sub

    ''' <summary>Reports (agreed 2026-10-31). Layouts wait for the Owner's own design and ADR 0005, so only Payslip has a screen (the payslip list).</summary>
    Private Sub AddReports()
        AddPlanned("reports.salarysheet", ReportsGroup, "Salary Sheet", ModuleKey.Reports, PlanFeature.BasicPayroll, 700)
        AddPlanned("reports.abstract", ReportsGroup, "Abstract", ModuleKey.Reports, PlanFeature.BasicPayroll, 710)
        AddDetailedReports()
        AddScreen("arrears.report", ReportsGroup, "Arrears report", ModuleKey.Payroll, PlanFeature.Arrears, 790, ArrearsReportScreen)
        AddCaReports()
    End Sub

    Private Sub AddDetailedReports()
        AddScreen("reports.detailed.payslip", DetailedGroup, "Payslip", ModuleKey.Payroll, PlanFeature.Payslips, 720, PayslipScreen)
        AddPlanned("reports.detailed.bonus", DetailedGroup, "Bonus", ModuleKey.Reports, PlanFeature.BasicPayroll, 730)
        AddPlanned("reports.detailed.careerhistory", DetailedGroup, "Career History", ModuleKey.Reports, PlanFeature.Employees, 740)
        AddPlanned("reports.detailed.loans", DetailedGroup, "Loans and Advances", ModuleKey.Reports, PlanFeature.Loans, 750)
        AddPlanned("reports.detailed.fnf", DetailedGroup, "F&F", ModuleKey.Reports, PlanFeature.FullAndFinal, 760)
        AddPlanned("reports.detailed.assets", DetailedGroup, "Assets and Recoveries", ModuleKey.Reports, PlanFeature.AssetsAndExpenses, 770)
        AddPlanned("reports.detailed.form130168", DetailedGroup, "Form 130/168", ModuleKey.Reports, PlanFeature.Form16And24Q, 780)
    End Sub

    Private Sub AddCaReports()
        AddPlanned("reports.ca.disbursement", CaReportsGroup, "Disbursement Annual", ModuleKey.Reports, PlanFeature.BasicPayroll, 800)
        AddPlanned("reports.ca.statutory", CaReportsGroup, "Statutory EPF/ESI", ModuleKey.Reports, PlanFeature.EpfEsi, 810)
        AddPlanned("reports.ca.loans", CaReportsGroup, "Loans and Advances", ModuleKey.Reports, PlanFeature.Loans, 820)
    End Sub

    ''' <summary>
    ''' Every screen a feature module offers must have a menu position, so a finished screen cannot be left out of the dashboard menu.
    ''' A missing position is a programming error found at start-up, like a duplicate key.
    ''' </summary>
    Private Sub RejectScreensWithoutPosition()
        Dim unplaced = _screens.Keys.Where(Function(key) Not _placedScreens.Contains(key)).ToList()
        If unplaced.Count > 0 Then
            Throw New InvalidOperationException("These screens have no menu position in MenuLayoutModule: " & String.Join(", ", unplaced))
        End If
    End Sub

    ''' <summary>A position with a screen. When the module that offers the screen is not registered the position is shown as planned.</summary>
    Private Sub AddScreen(key As String, group As String, title As String, moduleKey As ModuleKey, feature As PlanFeature?,
                       sortOrder As Integer, screenKey As String)
        Dim entry As New MenuEntry(key, group, title, moduleKey, feature, sortOrder)
        Dim source As ShellItem = Nothing
        If _screens.TryGetValue(screenKey, source) Then
            _items.Add(New ShellItem(entry, source.OpenScreen))
            _placedScreens.Add(screenKey)
        Else
            _items.Add(ShellItem.Planned(entry))
        End If
    End Sub

    Private Sub AddPlanned(key As String, group As String, title As String, moduleKey As ModuleKey, feature As PlanFeature?, sortOrder As Integer)
        _items.Add(ShellItem.Planned(New MenuEntry(key, group, title, moduleKey, feature, sortOrder)))
    End Sub
End Class
