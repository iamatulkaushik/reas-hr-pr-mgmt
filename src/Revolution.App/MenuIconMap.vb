Imports Revolution.UI.Common

''' <summary>
''' Which icon each menu position shows (ADR 0020). The key is a menu entry key or a group path as written in MenuLayoutModule.
''' A position not listed has no icon and shows text only; there is no placeholder. Add a line here when a menu position is added.
''' </summary>
Friend Module MenuIconMap
    Private ReadOnly Icons As New Dictionary(Of String, AppIcon)(StringComparer.Ordinal) From {
        {"Master", AppIcon.Masters},
        {"Master|Company", AppIcon.Company},
        {"Master|Company|Factory / Units", AppIcon.Location},
        {"master.company.statutory", AppIcon.Bank},
        {"master.company.units", AppIcon.Location},
        {"master.company.shifts", AppIcon.Clock},
        {"master.department", AppIcon.Department},
        {"master.designation", AppIcon.Designation},
        {"master.dailywages", AppIcon.Rupee},
        {"master.holidays", AppIcon.Holiday},
        {"Employee", AppIcon.Employee},
        {"Employee|Add Employee", AppIcon.Add},
        {"Employee|Update", AppIcon.Edit},
        {"employee.add.single", AppIcon.Employee},
        {"employee.add.import", AppIcon.Import},
        {"employee.update.statutory", AppIcon.Epf},
        {"employee.delete", AppIcon.Delete},
        {"Attendance", AppIcon.Attendance},
        {"Attendance|Insert", AppIcon.Add},
        {"attendance.insert.single", AppIcon.Present},
        {"attendance.update", AppIcon.Edit},
        {"attendance.monthclose", AppIcon.Lock},
        {"attendance.weeklyoff", AppIcon.Calendar},
        {"attendance.shiftassign", AppIcon.Clock},
        {"Payroll", AppIcon.Payroll},
        {"Payroll|Export", AppIcon.Export},
        {"payroll.export.ecr", AppIcon.Epf},
        {"payroll.export.esic", AppIcon.Esi},
        {"payroll.export.bank", AppIcon.BankFile},
        {"Administration", AppIcon.Settings},
        {"licence.manage", AppIcon.Key},
        {"tools.iconmap", AppIcon.Menu}
    }

    ''' <summary>The icon for a menu key, or Nothing when that position has none.</summary>
    Friend Function IconFor(key As String) As AppIcon?
        Dim icon As AppIcon
        If Icons.TryGetValue(key, icon) Then
            Return icon
        End If
        Return Nothing
    End Function
End Module
