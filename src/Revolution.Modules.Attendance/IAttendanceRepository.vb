''' <summary>Attendance storage. Implemented by SqlAttendanceRepository (stored procedures only). Rule breaks raise AttendanceRuleException.</summary>
Public Interface IAttendanceRepository
    ''' <summary>Month keys (for example 202610) that are closed for the signed-in company.</summary>
    Function ListLockedMonths() As ISet(Of Integer)

    Function ListMonth(employeeId As Integer, year As Integer, month As Integer) As IReadOnlyList(Of DayRecord)

    Sub SaveDay(employeeId As Integer, record As DayRecord, shiftId As Integer?, workedMinutes As Integer, userId As Integer?)

    Sub CloseMonth(monthKey As Integer, userId As Integer?)

    ''' <summary>Unlocks the month and writes the history row in one transaction.</summary>
    Sub ReopenMonth(monthKey As Integer, reason As String, userId As Integer?)
End Interface
