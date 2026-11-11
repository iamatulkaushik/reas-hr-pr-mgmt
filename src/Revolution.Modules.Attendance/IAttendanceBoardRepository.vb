''' <summary>Read side of the attendance screens (migration 0208). Writes stay in IAttendanceRepository.</summary>
Public Interface IAttendanceBoardRepository
    Function ListDay(day As Date) As IReadOnlyList(Of AttendanceSheetRow)

    ''' <summary>Every employee on the roll in the month, with each marked day. An unmarked employee has one row with no day.</summary>
    Function ListMonthAll(year As Integer, month As Integer) As IReadOnlyList(Of MonthDayRow)

    ''' <summary>One employee's marked days in the month, with shift and worked minutes (migration 0209).</summary>
    Function ListEmployeeMonth(employeeId As Integer, year As Integer, month As Integer) As IReadOnlyList(Of EmployeeDayRow)

    Function ListMonthEvents(monthKey As Integer) As IReadOnlyList(Of MonthLockEntry)
End Interface
