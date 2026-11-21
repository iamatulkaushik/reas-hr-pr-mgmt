''' <summary>Monthly attendance storage (migrations 0210 to 0214). Rule breaks raise AttendanceRuleException.</summary>
Public Interface IAttendanceMonthlyRepository
    ''' <summary>Every active employee employed in the month, with the saved counts or empty counts. periodStart is the first of the month.</summary>
    Function ListMonth(periodStart As Date) As IReadOnlyList(Of MonthlyAttendanceRow)

    ''' <summary>Upserts one employee's month. requireComplete makes the database refuse counts that do not add up to the days of employment.</summary>
    Sub Save(periodStart As Date, row As MonthlyAttendanceRow, requireComplete As Boolean, userId As Integer?)

    Sub Delete(employeeId As Integer, periodStart As Date, userId As Integer?)

    ''' <summary>The stored mode; Monthly when the company never chose.</summary>
    Function GetMode() As AttendanceMode

    Sub SetMode(mode As AttendanceMode)
End Interface
