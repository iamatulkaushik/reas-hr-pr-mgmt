Imports System.Globalization
Imports Revolution.Core

''' <summary>Screen use cases: the day sheet, the close sheet and the close history. Marking and closing go through AttendanceService.</summary>
Public NotInheritable Class AttendanceBoardService
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const MaxErrorsShown As Integer = 5

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
