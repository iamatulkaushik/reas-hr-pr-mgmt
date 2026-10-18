''' <summary>Shifts, holidays, weekly off and shift assignment (att procedures of 0202 and 0206). Rule breaks raise AttendanceRuleException.</summary>
Public Interface IRosterRepository
    Function ListShifts() As IReadOnlyList(Of ShiftItem)

    ''' <summary>Inserts when ShiftId is Nothing, else updates. Returns the shift id.</summary>
    Function SaveShift(item As ShiftItem) As Integer

    Function ListHolidays(year As Integer) As IReadOnlyList(Of HolidayItem)

    Sub SaveHoliday(item As HolidayItem)

    Sub DeleteHoliday(day As Date)

    Function ListWeeklyOff() As IReadOnlyList(Of WeeklyOffRule)

    Sub SaveWeeklyOff(rule As WeeklyOffRule)

    Function ListEmployeeShifts(employeeId As Integer) As IReadOnlyList(Of EmployeeShiftItem)

    Sub AssignShift(employeeId As Integer, shiftId As Integer, effectiveFrom As Date)
End Interface
