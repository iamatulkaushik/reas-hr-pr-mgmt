Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' Checks and saves one row of the monthly attendance grid and import (ADR 0025, ADR 0026). A row is an upsert: the same sheet again replaces the month's rows.
''' A blank Weekly Off Days or Holiday Days cell takes the company rule; any other blank count is zero. Counts must add up to the days of employment.
''' Messages name the column, never the cell. A closed month is refused. After a run, MissingFromSheet lists the employees the sheet did not mention.
''' </summary>
Public NotInheritable Class MonthlyAttendanceHandler
    Implements IBulkRowHandler

    Private Const MaxDay As Integer = 31
    Private Const MaxOvertimeHours As Decimal = 744D

    Private ReadOnly _service As AttendanceMonthlyService
    Private ReadOnly _period As Date
    Private ReadOnly _seen As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly _roll As New Dictionary(Of String, MonthlyAttendanceRow)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly _rules As New Dictionary(Of Integer, RuleCounts)()
    Private _loaded As Boolean
    Private _loadError As String = String.Empty

    Public Sub New(service As AttendanceMonthlyService, periodStart As Date)
        If service Is Nothing Then Throw New ArgumentNullException(NameOf(service))
        _service = service
        _period = New Date(periodStart.Year, periodStart.Month, 1)
    End Sub

    Public Function Validate(row As ImportRow) As String Implements IBulkRowHandler.Validate
        Dim parsed As ParsedRow = Nothing
        Return Parse(row, parsed)
    End Function

    Public Function RowKey(row As ImportRow) As String Implements IBulkRowHandler.RowKey
        Return row.Get(MonthlyAttendanceTemplate.ColCode).Trim()
    End Function

    Public Function Save(row As ImportRow) As OperationResult(Of String) Implements IBulkRowHandler.Save
        Dim parsed As ParsedRow = Nothing
        Dim problem = Parse(row, parsed)
        If problem.Length > 0 Then Return OperationResult(Of String).Fail(problem)
        Dim result = _service.Save(_period, parsed.Person.WithCounts(parsed.Present, parsed.Half, parsed.WeeklyOff, parsed.Holiday, parsed.PaidLeave, parsed.Absent, parsed.Overtime, parsed.Person.Remarks), True)
        If Not result.Succeeded Then Return OperationResult(Of String).Fail(result.Message)
        Return OperationResult(Of String).Ok("Saved")
    End Function

    ''' <summary>Employee codes (up to limit) of the month's employees that no row of the sheet named, and the total count. A missing employee blocks the month close.</summary>
    Public Function MissingFromSheet(limit As Integer, ByRef total As Integer) As IReadOnlyList(Of String)
        EnsureLoaded()
        Dim missing = _roll.Values.Where(Function(r) Not _seen.Contains(r.EmpCode)).OrderBy(Function(r) r.EmpCode, StringComparer.Ordinal).Select(Function(r) r.EmpCode).ToList()
        total = missing.Count
        Return missing.Take(Math.Max(limit, 0)).ToList()
    End Function

    Private Structure ParsedRow
        Public Person As MonthlyAttendanceRow
        Public Present As Integer
        Public Half As Integer
        Public WeeklyOff As Integer
        Public Holiday As Integer
        Public PaidLeave As Integer
        Public Absent As Integer
        Public Overtime As Decimal
    End Structure

    Private Sub EnsureLoaded()
        If _loaded Then Return
        _loaded = True
        Dim rows = _service.LoadMonth(_period)
        If Not rows.Succeeded Then
            _loadError = rows.Message
            Return
        End If
        For Each r In rows.Value
            _roll(r.EmpCode) = r
        Next
    End Sub

    Private Function Parse(row As ImportRow, ByRef parsed As ParsedRow) As String
        EnsureLoaded()
        If _loadError.Length > 0 Then Return _loadError
        If Not _service.CanEdit() Then Return "You are not allowed to mark attendance."
        Dim code = row.Get(MonthlyAttendanceTemplate.ColCode).Trim()
        If code.Length = 0 Then Return $"{MonthlyAttendanceTemplate.ColCode} is empty."
        _seen.Add(code)
        Dim person As MonthlyAttendanceRow = Nothing
        If Not _roll.TryGetValue(code, person) Then Return $"{MonthlyAttendanceTemplate.ColCode} is not on the roll in this month."
        parsed.Person = person
        If _service.IsClosed(_period) Then Return "The month is closed."
        Dim problem = ReadCount(row, MonthlyAttendanceTemplate.ColPresent, True, 0, parsed.Present)
        If problem.Length > 0 Then Return problem
        problem = ReadCount(row, MonthlyAttendanceTemplate.ColHalf, False, 0, parsed.Half)
        If problem.Length > 0 Then Return problem
        problem = ReadCount(row, MonthlyAttendanceTemplate.ColPaidLeave, False, 0, parsed.PaidLeave)
        If problem.Length > 0 Then Return problem
        problem = ReadCount(row, MonthlyAttendanceTemplate.ColAbsent, False, 0, parsed.Absent)
        If problem.Length > 0 Then Return problem
        Dim defaults As RuleCounts = Nothing
        If row.Get(MonthlyAttendanceTemplate.ColWeeklyOff).Length = 0 OrElse row.Get(MonthlyAttendanceTemplate.ColHoliday).Length = 0 Then
            If Not _rules.TryGetValue(person.EmployeeId, defaults) Then
                Dim looked = _service.RuleDefaults(person)
                If Not looked.Succeeded Then Return looked.Message
                defaults = looked.Value
                _rules(person.EmployeeId) = defaults
            End If
        End If
        problem = ReadCount(row, MonthlyAttendanceTemplate.ColWeeklyOff, False, If(defaults Is Nothing, 0, defaults.WeeklyOffDays), parsed.WeeklyOff)
        If problem.Length > 0 Then Return problem
        problem = ReadCount(row, MonthlyAttendanceTemplate.ColHoliday, False, If(defaults Is Nothing, 0, defaults.HolidayDays), parsed.Holiday)
        If problem.Length > 0 Then Return problem
        Dim hoursText = row.Get(MonthlyAttendanceTemplate.ColOvertime)
        parsed.Overtime = 0D
        If hoursText.Length > 0 Then
            If Not ImportValue.TryDecimal(hoursText, parsed.Overtime) Then Return $"{MonthlyAttendanceTemplate.ColOvertime} is not a number."
            If parsed.Overtime < 0D OrElse parsed.Overtime > MaxOvertimeHours Then Return $"{MonthlyAttendanceTemplate.ColOvertime} must be between 0 and 744."
            If Decimal.Round(parsed.Overtime, 2) <> parsed.Overtime Then Return $"{MonthlyAttendanceTemplate.ColOvertime} can have two decimals at most."
        End If
        Dim total = parsed.Present + parsed.Half + parsed.WeeklyOff + parsed.Holiday + parsed.PaidLeave + parsed.Absent
        If total > person.WindowDays Then
            Return $"Days add up to {total.ToString(CultureInfo.InvariantCulture)} but the employee worked {person.WindowDays.ToString(CultureInfo.InvariantCulture)} days in the month."
        End If
        If total <> person.WindowDays Then
            Return $"Days add up to {total.ToString(CultureInfo.InvariantCulture)} of {person.WindowDays.ToString(CultureInfo.InvariantCulture)}. Fill Absent Days or the missing days."
        End If
        Return String.Empty
    End Function

    ''' <summary>A whole number from 0 to 31. A blank optional cell takes the fallback; a blank required cell is a problem.</summary>
    Private Shared Function ReadCount(row As ImportRow, column As String, required As Boolean, fallback As Integer, ByRef value As Integer) As String
        Dim text = row.Get(column)
        If text.Length = 0 Then
            If required Then Return $"{column} is empty."
            value = fallback
            Return String.Empty
        End If
        If Not ImportValue.TryWholeNumber(text, value) Then Return $"{column} is not a whole number."
        If value < 0 OrElse value > MaxDay Then Return $"{column} must be between 0 and 31."
        Return String.Empty
    End Function
End Class
