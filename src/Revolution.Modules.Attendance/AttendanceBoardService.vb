Imports System.Globalization
Imports Revolution.Core

''' <summary>Screen use cases: the day sheet, the close sheet and the close history. Marking and closing go through AttendanceService.</summary>
Public NotInheritable Class AttendanceBoardService
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const MaxErrorsShown As Integer = 5
    Private Const MaxRangeDays As Integer = 31
    Private Const DateFormat As String = "dd-MM-yyyy"

    Private ReadOnly _board As IAttendanceBoardRepository
    Private ReadOnly _attendance As AttendanceService
    Private ReadOnly _repository As IAttendanceRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _today As Func(Of Date)

    Public Sub New(board As IAttendanceBoardRepository, attendance As AttendanceService, repository As IAttendanceRepository,
                   permissions As IPermissionService, today As Func(Of Date))
        If board Is Nothing Then Throw New ArgumentNullException(NameOf(board))
        If attendance Is Nothing Then Throw New ArgumentNullException(NameOf(attendance))
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If today Is Nothing Then Throw New ArgumentNullException(NameOf(today))
        _board = board
        _attendance = attendance
        _repository = repository
        _permissions = permissions
        _today = today
    End Sub

    Public Function LoadDay(day As Date) As OperationResult(Of IReadOnlyList(Of AttendanceSheetRow))
        If Not _permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.View) Then
            Return OperationResult(Of IReadOnlyList(Of AttendanceSheetRow)).Fail(NotAllowed)
        End If
        Return OperationResult(Of IReadOnlyList(Of AttendanceSheetRow)).Ok(_board.ListDay(day.Date))
    End Function

    ''' <summary>Saves every row that has a status. Rows without one stay unmarked. Stops listing after a few errors.</summary>
    Public Function SaveDay(day As Date, rows As IEnumerable(Of AttendanceSheetRow)) As OperationResult
        If rows Is Nothing Then Throw New ArgumentNullException(NameOf(rows))
        Dim errors As New List(Of String)()
        Dim saved As Integer = 0
        For Each row In rows.Where(Function(r) r.Status.HasValue)
            Dim result = _attendance.MarkDay(row.EmployeeId, day, row.Status.Value, row.WorkedMinutes, row.OvertimeMinutes, row.ShiftId)
            If result.Succeeded Then
                saved += 1
            Else
                errors.Add($"{row.EmpCode}: {result.Message}")
            End If
        Next
        If errors.Count = 0 Then Return OperationResult.Ok()
        Dim shown = String.Join(Environment.NewLine, errors.Take(MaxErrorsShown))
        If errors.Count > MaxErrorsShown Then shown &= Environment.NewLine & $"...and {errors.Count - MaxErrorsShown} more."
        Return OperationResult.Fail($"{saved} saved, {errors.Count} not saved:{Environment.NewLine}{shown}")
    End Function

    ''' <summary>
    ''' Marks the same status for several employees over a date range (at most 31 days). Employees not on the roll that day are skipped.
    ''' A day that is already marked stays as it is unless overwrite is set; an overwritten day keeps its shift and worked minutes.
    ''' Returns the number of days saved. A closed month is refused once per month, not once per employee.
    ''' </summary>
    Public Function MarkMultiple(fromDay As Date, toDay As Date, employeeIds As IEnumerable(Of Integer), status As AttendanceStatus,
                                 overtimeMinutes As Integer, overwrite As Boolean) As OperationResult(Of Integer)
        If employeeIds Is Nothing Then Throw New ArgumentNullException(NameOf(employeeIds))
        If Not _permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Edit) Then
            Return OperationResult(Of Integer).Fail(NotAllowed)
        End If
        Dim ids = employeeIds.Distinct().ToList()
        If ids.Count = 0 Then Return OperationResult(Of Integer).Fail("Select at least one employee.")
        If fromDay.Date > toDay.Date Then Return OperationResult(Of Integer).Fail("From date is after To date.")
        If toDay.Date > _today().Date Then Return OperationResult(Of Integer).Fail("You cannot mark a future day.")
        Dim span = (toDay.Date - fromDay.Date).Days
        If span >= MaxRangeDays Then Return OperationResult(Of Integer).Fail($"Pick at most {MaxRangeDays} days.")

        Dim locked = _repository.ListLockedMonths()
        Dim refused As New HashSet(Of Integer)()
        Dim errors As New List(Of String)()
        Dim saved As Integer = 0
        For offset As Integer = 0 To span
            Dim day = fromDay.Date.AddDays(offset)
            If MonthLockPolicy.IsLocked(day, locked) Then
                If refused.Add(MonthLockPolicy.KeyOf(day)) Then errors.Add($"{day.ToString("MM-yyyy", CultureInfo.InvariantCulture)}: The month is closed.")
                Continue For
            End If
            Dim roll = _board.ListDay(day).GroupBy(Function(r) r.EmployeeId).ToDictionary(Function(g) g.Key, Function(g) g.First())
            For Each id In ids
                Dim row As AttendanceSheetRow = Nothing
                If Not roll.TryGetValue(id, row) Then Continue For
                If row.Status.HasValue AndAlso Not overwrite Then Continue For
                Dim shiftId As Integer? = If(row.Status.HasValue, row.ShiftId, CType(Nothing, Integer?))
                Dim worked As Integer = If(row.Status.HasValue, row.WorkedMinutes, 0)
                Dim result = _attendance.MarkDay(id, day, status, worked, overtimeMinutes, shiftId)
                If result.Succeeded Then
                    saved += 1
                Else
                    errors.Add($"{row.EmpCode} {day.ToString(DateFormat, CultureInfo.InvariantCulture)}: {result.Message}")
                End If
            Next
        Next
        If errors.Count = 0 Then Return OperationResult(Of Integer).Ok(saved)
        Dim shown = String.Join(Environment.NewLine, errors.Take(MaxErrorsShown))
        If errors.Count > MaxErrorsShown Then shown &= Environment.NewLine & $"...and {errors.Count - MaxErrorsShown} more."
        Return OperationResult(Of Integer).Fail($"{saved} saved, {errors.Count} not saved:{Environment.NewLine}{shown}")
    End Function

    ''' <summary>Deletes the marked day of each chosen employee on one day. Returns how many were deleted. Needs the Delete permission.</summary>
    Public Function DeleteDays(day As Date, employeeIds As IEnumerable(Of Integer)) As OperationResult(Of Integer)
        If employeeIds Is Nothing Then Throw New ArgumentNullException(NameOf(employeeIds))
        If Not _permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Delete) Then
            Return OperationResult(Of Integer).Fail(NotAllowed)
        End If
        Dim ids = employeeIds.Distinct().ToList()
        If ids.Count = 0 Then Return OperationResult(Of Integer).Fail("Tick at least one day to delete.")
        Dim codes = _board.ListDay(day.Date).GroupBy(Function(r) r.EmployeeId).ToDictionary(Function(g) g.Key, Function(g) g.First().EmpCode)
        Dim errors As New List(Of String)()
        Dim deleted As Integer = 0
        For Each id In ids
            Dim result = _attendance.DeleteDay(id, day.Date)
            If result.Succeeded Then
                deleted += 1
            Else
                Dim code As String = Nothing
                If Not codes.TryGetValue(id, code) Then code = "?"
                errors.Add($"{code}: {result.Message}")
            End If
        Next
        If errors.Count = 0 Then Return OperationResult(Of Integer).Ok(deleted)
        Dim shown = String.Join(Environment.NewLine, errors.Take(MaxErrorsShown))
        If errors.Count > MaxErrorsShown Then shown &= Environment.NewLine & $"...and {errors.Count - MaxErrorsShown} more."
        Return OperationResult(Of Integer).Fail($"{deleted} deleted, {errors.Count} not deleted:{Environment.NewLine}{shown}")
    End Function

    ''' <summary>One employee's marked days in a month, with shift and worked minutes.</summary>
    Public Function LoadEmployeeMonth(employeeId As Integer, year As Integer, month As Integer) As OperationResult(Of IReadOnlyList(Of EmployeeDayRow))
        If Not _permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.View) Then
            Return OperationResult(Of IReadOnlyList(Of EmployeeDayRow)).Fail(NotAllowed)
        End If
        If employeeId <= 0 Then Return OperationResult(Of IReadOnlyList(Of EmployeeDayRow)).Fail("Pick an employee.")
        If month < 1 OrElse month > 12 OrElse year < 1 OrElse year > 9999 Then
            Return OperationResult(Of IReadOnlyList(Of EmployeeDayRow)).Fail("Month or year is not valid.")
        End If
        Return OperationResult(Of IReadOnlyList(Of EmployeeDayRow)).Ok(_board.ListEmployeeMonth(employeeId, year, month))
    End Function

    ''' <summary>Saves the given days of one employee, keeping each day's shift and worked minutes. Stops listing after a few errors.</summary>
    Public Function SaveEmployeeMonth(employeeId As Integer, rows As IEnumerable(Of EmployeeDayRow)) As OperationResult
        If rows Is Nothing Then Throw New ArgumentNullException(NameOf(rows))
        Dim errors As New List(Of String)()
        Dim saved As Integer = 0
        For Each row In rows
            Dim result = _attendance.MarkDay(employeeId, row.Day, row.Status, row.WorkedMinutes, row.OvertimeMinutes, row.ShiftId)
            If result.Succeeded Then
                saved += 1
            Else
                errors.Add($"{row.Day.ToString(DateFormat, CultureInfo.InvariantCulture)}: {result.Message}")
            End If
        Next
        If errors.Count = 0 Then Return OperationResult.Ok()
        Dim shown = String.Join(Environment.NewLine, errors.Take(MaxErrorsShown))
        If errors.Count > MaxErrorsShown Then shown &= Environment.NewLine & $"...and {errors.Count - MaxErrorsShown} more."
        Return OperationResult.Fail($"{saved} saved, {errors.Count} not saved:{Environment.NewLine}{shown}")
    End Function

    ''' <summary>The row handler for the Multiple and Import list (ADR 0025).</summary>
    Public Function CreateBulkHandler() As IBulkRowHandler
        Return New AttendanceBulkHandler(_board, _attendance, _permissions, _today)
    End Function

    ''' <summary>The columns of the Multiple and Import list.</summary>
    Public Shared Function BulkTemplate() As ImportTemplate
        Return AttendanceBulkTemplate.Create()
    End Function

    Public Function LoadCloseSheet(year As Integer, month As Integer) As OperationResult(Of CloseSheet)
        If Not _permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.View) Then
            Return OperationResult(Of CloseSheet).Fail(NotAllowed)
        End If
        If month < 1 OrElse month > 12 OrElse year < 1 OrElse year > 9999 Then
            Return OperationResult(Of CloseSheet).Fail("Month or year is not valid.")
        End If
        Dim summaries As New Dictionary(Of Integer, MonthSummary)()
        Dim blockers As New List(Of String)()
        For Each group In _board.ListMonthAll(year, month).GroupBy(Function(r) r.EmployeeId)
            Dim first = group.First()
            Dim built = MonthSummaryBuilder.Build(year, month, group.Where(Function(r) r.Day IsNot Nothing).Select(Function(r) r.Day))
            If Not built.Succeeded Then
                blockers.Add($"{first.EmpCode} {first.EmployeeName}: {built.Message}")
                Continue For
            End If
            summaries(group.Key) = built.Value
            Dim check = MonthLockPolicy.CanClose(built.Value, _today())
            If Not check.Succeeded Then blockers.Add($"{first.EmpCode} {first.EmployeeName}: {check.Message}")
        Next
        Dim closed = MonthLockPolicy.IsLocked(New Date(year, month, 1), _repository.ListLockedMonths())
        Return OperationResult(Of CloseSheet).Ok(New CloseSheet(year, month, summaries, blockers, closed))
    End Function

    ''' <summary>Rebuilds the sheet and closes only when no employee blocks it.</summary>
    Public Function CloseMonth(year As Integer, month As Integer) As OperationResult
        Dim sheet = LoadCloseSheet(year, month)
        If Not sheet.Succeeded Then Return OperationResult.Fail(sheet.Message)
        If sheet.Value.IsClosed Then Return OperationResult.Fail("The month is already closed.")
        If sheet.Value.Blockers.Count > 0 Then Return OperationResult.Fail(sheet.Value.Blockers(0))
        Return _attendance.CloseMonth(year, month, sheet.Value.Summaries)
    End Function

    Public Function ReopenMonth(year As Integer, month As Integer, reason As String) As OperationResult
        Return _attendance.ReopenMonth(year, month, reason)
    End Function

    Public Function LoadHistory(year As Integer, month As Integer) As OperationResult(Of IReadOnlyList(Of MonthLockEntry))
        If Not _permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.View) Then
            Return OperationResult(Of IReadOnlyList(Of MonthLockEntry)).Fail(NotAllowed)
        End If
        Dim key = year * 100 + month
        Return OperationResult(Of IReadOnlyList(Of MonthLockEntry)).Ok(_board.ListMonthEvents(key))
    End Function
End Class
