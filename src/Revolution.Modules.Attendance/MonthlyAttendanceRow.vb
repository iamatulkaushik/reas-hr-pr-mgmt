''' <summary>One employee's attendance for one calendar month (att.AttendanceMonthly, ADR 0026), with the days the employee was employed in that month.</summary>
Public NotInheritable Class MonthlyAttendanceRow
    Public Sub New(employeeId As Integer, empCode As String, employeeName As String, windowFrom As Date, windowTo As Date, windowDays As Integer, hasRow As Boolean,
                   presentDays As Integer, halfDays As Integer, weeklyOffDays As Integer, holidayDays As Integer, paidLeaveDays As Integer, absentDays As Integer,
                   overtimeHours As Decimal, remarks As String)
        Me.EmployeeId = employeeId
        Me.EmpCode = If(empCode, String.Empty)
        Me.EmployeeName = If(employeeName, String.Empty)
        Me.WindowFrom = windowFrom.Date
        Me.WindowTo = windowTo.Date
        Me.WindowDays = windowDays
        Me.HasRow = hasRow
        Me.PresentDays = presentDays
        Me.HalfDays = halfDays
        Me.WeeklyOffDays = weeklyOffDays
        Me.HolidayDays = holidayDays
        Me.PaidLeaveDays = paidLeaveDays
        Me.AbsentDays = absentDays
        Me.OvertimeHours = overtimeHours
        Me.Remarks = If(remarks, String.Empty)
    End Sub

    Public ReadOnly Property EmployeeId As Integer
    Public ReadOnly Property EmpCode As String
    Public ReadOnly Property EmployeeName As String
    ''' <summary>First and last day the employee was employed in the month.</summary>
    Public ReadOnly Property WindowFrom As Date
    Public ReadOnly Property WindowTo As Date
    Public ReadOnly Property WindowDays As Integer
    ''' <summary>False for an employee with no saved row (the counts are then all zero).</summary>
    Public ReadOnly Property HasRow As Boolean
    Public ReadOnly Property PresentDays As Integer
    Public ReadOnly Property HalfDays As Integer
    Public ReadOnly Property WeeklyOffDays As Integer
    Public ReadOnly Property HolidayDays As Integer
    Public ReadOnly Property PaidLeaveDays As Integer
    Public ReadOnly Property AbsentDays As Integer
    Public ReadOnly Property OvertimeHours As Decimal
    Public ReadOnly Property Remarks As String

    ''' <summary>Every counted day. A half day is one day here; it is paid as half in payroll.</summary>
    Public ReadOnly Property TotalDays As Integer
        Get
            Return PresentDays + HalfDays + WeeklyOffDays + HolidayDays + PaidLeaveDays + AbsentDays
        End Get
    End Property

    ''' <summary>A saved row whose counts add up to the days of employment.</summary>
    Public ReadOnly Property IsComplete As Boolean
        Get
            Return HasRow AndAlso TotalDays = WindowDays
        End Get
    End Property

    ''' <summary>The same row with other counts (used when a screen or the import fills the values).</summary>
    Public Function WithCounts(presentDays As Integer, halfDays As Integer, weeklyOffDays As Integer, holidayDays As Integer, paidLeaveDays As Integer,
                               absentDays As Integer, overtimeHours As Decimal, remarks As String) As MonthlyAttendanceRow
        Return New MonthlyAttendanceRow(EmployeeId, EmpCode, EmployeeName, WindowFrom, WindowTo, WindowDays, True, presentDays, halfDays, weeklyOffDays,
                                        holidayDays, paidLeaveDays, absentDays, overtimeHours, remarks)
    End Function
End Class
