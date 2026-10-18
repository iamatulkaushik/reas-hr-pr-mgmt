Imports Revolution.Modules.Attendance

Friend NotInheritable Class FakeRosterRepository
    Implements IRosterRepository

    Public ReadOnly SavedShifts As New List(Of ShiftItem)()
    Public ReadOnly SavedHolidays As New List(Of HolidayItem)()
    Public ReadOnly Assigned As New List(Of String)()
    Public FailWith As String

    Public Function ListShifts() As IReadOnlyList(Of ShiftItem) Implements IRosterRepository.ListShifts
        Return SavedShifts
    End Function

    Public Function SaveShift(item As ShiftItem) As Integer Implements IRosterRepository.SaveShift
        ThrowIfFailing()
        SavedShifts.Add(item)
        Return 11
    End Function

    Public Function ListHolidays(year As Integer) As IReadOnlyList(Of HolidayItem) Implements IRosterRepository.ListHolidays
        Return SavedHolidays
    End Function

    Public Sub SaveHoliday(item As HolidayItem) Implements IRosterRepository.SaveHoliday
        ThrowIfFailing()
        SavedHolidays.Add(item)
    End Sub

    Public Sub DeleteHoliday(day As Date) Implements IRosterRepository.DeleteHoliday
        ThrowIfFailing()
        SavedHolidays.RemoveAll(Function(h) h.Day = day.Date)
    End Sub

    Public Function ListWeeklyOff() As IReadOnlyList(Of WeeklyOffRule) Implements IRosterRepository.ListWeeklyOff
        Return New List(Of WeeklyOffRule)()
    End Function

    Public Sub SaveWeeklyOff(rule As WeeklyOffRule) Implements IRosterRepository.SaveWeeklyOff
        ThrowIfFailing()
    End Sub

    Public Function ListEmployeeShifts(employeeId As Integer) As IReadOnlyList(Of EmployeeShiftItem) Implements IRosterRepository.ListEmployeeShifts
        Return New List(Of EmployeeShiftItem)()
    End Function

    Public Sub AssignShift(employeeId As Integer, shiftId As Integer, effectiveFrom As Date) Implements IRosterRepository.AssignShift
        ThrowIfFailing()
        Assigned.Add($"{employeeId}:{shiftId}:{effectiveFrom:yyyyMMdd}")
    End Sub

    Private Sub ThrowIfFailing()
        If FailWith IsNot Nothing Then Throw New AttendanceRuleException(FailWith)
    End Sub
End Class

Friend NotInheritable Class FakeBoardRepository
    Implements IAttendanceBoardRepository

    Public ReadOnly Day As New List(Of AttendanceSheetRow)()
    Public ReadOnly Month As New List(Of MonthDayRow)()

    Public Function ListDay(day As Date) As IReadOnlyList(Of AttendanceSheetRow) Implements IAttendanceBoardRepository.ListDay
        Return Me.Day
    End Function

    Public Function ListMonthAll(year As Integer, month As Integer) As IReadOnlyList(Of MonthDayRow) Implements IAttendanceBoardRepository.ListMonthAll
        Return Me.Month
    End Function

    Public Function ListMonthEvents(monthKey As Integer) As IReadOnlyList(Of MonthLockEntry) Implements IAttendanceBoardRepository.ListMonthEvents
        Return New List(Of MonthLockEntry)()
    End Function
End Class
