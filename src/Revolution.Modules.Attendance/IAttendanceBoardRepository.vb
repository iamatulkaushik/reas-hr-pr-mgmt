''' <summary>Read side of the attendance screens (migration 0208). Writes stay in IAttendanceRepository.</summary>
Public Interface IAttendanceBoardRepository
    Function ListDay(day As Date) As IReadOnlyList(Of AttendanceSheetRow)

    ''' <summary>Every employee on the roll in the month, with each marked day. An unmarked employee has one row with no day.</summary>
    Function ListMonthAll(year As Integer, month As Integer) As IReadOnlyList(Of MonthDayRow)

    Function ListMonthEvents(monthKey As Integer) As IReadOnlyList(Of MonthLockEntry)
End Interface
