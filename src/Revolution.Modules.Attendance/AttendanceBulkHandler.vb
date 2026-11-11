Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' Checks and saves one attendance row of the Multiple and Import list (ADR 0025). A row creates a day: a day that is already marked is refused, never changed.
''' Messages name the column, never the cell. The roll of a day is read once and kept for Validate; Save reads it again, so a day marked meanwhile is not overwritten.
''' </summary>
Friend NotInheritable Class AttendanceBulkHandler
    Implements IBulkRowHandler

    Private Const MinutesPerHour As Integer = 60
    Private Const MaxHours As Decimal = 24D
    Private Const KeyFormat As String = "yyyyMMdd"

    Private ReadOnly _board As IAttendanceBoardRepository
    Private ReadOnly _attendance As AttendanceService
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _today As Func(Of Date)
    Private ReadOnly _rolls As New Dictionary(Of Date, Dictionary(Of String, AttendanceSheetRow))()

    Public Sub New(board As IAttendanceBoardRepository, attendance As AttendanceService, permissions As IPermissionService, today As Func(Of Date))
        If board Is Nothing Then Throw New ArgumentNullException(NameOf(board))
        If attendance Is Nothing Then Throw New ArgumentNullException(NameOf(attendance))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If today Is Nothing Then Throw New ArgumentNullException(NameOf(today))
        _board = board
        _attendance = attendance
        _permissions = permissions
        _today = today
    End Sub

    Public Function Validate(row As ImportRow) As String Implements IBulkRowHandler.Validate
        Dim parsed As ParsedRow = Nothing
        Dim problem = Parse(row, parsed)
        If problem.Length > 0 Then Return problem
        Dim person As AttendanceSheetRow = Nothing
        If Not Lookup(parsed.Day, False).TryGetValue(parsed.Code, person) Then
            Return $"{AttendanceBulkTemplate.ColCode} is not on the roll on that date."
        End If
        If person.Status.HasValue Then Return "That day is already marked."
        Return String.Empty
    End Function

    Public Function RowKey(row As ImportRow) As String Implements IBulkRowHandler.RowKey
        Dim code = row.Get(AttendanceBulkTemplate.ColCode).Trim()
        Dim day As Date
        If code.Length = 0 OrElse Not ImportValue.TryDate(row.Get(AttendanceBulkTemplate.ColDate), day) Then Return String.Empty
        Return code & "|" & day.ToString(KeyFormat, CultureInfo.InvariantCulture)
    End Function

    Public Function Save(row As ImportRow) As OperationResult(Of String) Implements IBulkRowHandler.Save
        Dim parsed As ParsedRow = Nothing
        Dim problem = Parse(row, parsed)
        If problem.Length > 0 Then Return OperationResult(Of String).Fail(problem)
        Dim person As AttendanceSheetRow = Nothing
        If Not Lookup(parsed.Day, True).TryGetValue(parsed.Code, person) Then
            Return OperationResult(Of String).Fail($"{AttendanceBulkTemplate.ColCode} is not on the roll on that date.")
        End If
        If person.Status.HasValue Then Return OperationResult(Of String).Fail("That day is already marked.")
        Dim result = _attendance.MarkDay(person.EmployeeId, parsed.Day, parsed.Status, 0, parsed.OvertimeMinutes, Nothing)
        If Not result.Succeeded Then Return OperationResult(Of String).Fail(result.Message)
        _rolls.Remove(parsed.Day)
        Return OperationResult(Of String).Ok("Saved")
    End Function

    Private Structure ParsedRow
        Public Code As String
        Public Day As Date
        Public Status As AttendanceStatus
        Public OvertimeMinutes As Integer
    End Structure

    Private Function Parse(row As ImportRow, ByRef parsed As ParsedRow) As String
        If Not _permissions.IsAllowed(ModuleKey.Attendance, PermissionAction.Edit) Then Return "You are not allowed to mark attendance."
        parsed.Code = row.Get(AttendanceBulkTemplate.ColCode).Trim()
        If parsed.Code.Length = 0 Then Return $"{AttendanceBulkTemplate.ColCode} is empty."
        If Not ImportValue.TryDate(row.Get(AttendanceBulkTemplate.ColDate), parsed.Day) Then Return $"{AttendanceBulkTemplate.ColDate} is not a valid date."
        If parsed.Day.Date > _today().Date Then Return $"{AttendanceBulkTemplate.ColDate} is in the future."
        If Not TryStatus(row.Get(AttendanceBulkTemplate.ColStatus), parsed.Status) Then Return $"{AttendanceBulkTemplate.ColStatus} must be P, A, HD, WO, H or PL."
        Dim hoursText = row.Get(AttendanceBulkTemplate.ColOvertime).Trim()
        parsed.OvertimeMinutes = 0
        If hoursText.Length > 0 Then
            Dim hours As Decimal
            If Not ImportValue.TryDecimal(hoursText, hours) OrElse hours < 0D OrElse hours > MaxHours Then Return $"{AttendanceBulkTemplate.ColOvertime} must be hours from 0 to 24."
            parsed.OvertimeMinutes = CInt(Math.Round(hours * MinutesPerHour, 0, MidpointRounding.AwayFromZero))
        End If
        Return String.Empty
    End Function

    Private Function Lookup(day As Date, fresh As Boolean) As Dictionary(Of String, AttendanceSheetRow)
        Dim roll As Dictionary(Of String, AttendanceSheetRow) = Nothing
        If fresh OrElse Not _rolls.TryGetValue(day.Date, roll) Then
            roll = New Dictionary(Of String, AttendanceSheetRow)(StringComparer.OrdinalIgnoreCase)
            For Each person In _board.ListDay(day.Date)
                roll(person.EmpCode) = person
            Next
            _rolls(day.Date) = roll
        End If
        Return roll
    End Function

    Private Shared Function TryStatus(text As String, ByRef status As AttendanceStatus) As Boolean
        Select Case ImportTemplate.Normalize(text)
            Case "p", "present" : status = AttendanceStatus.Present
            Case "a", "absent" : status = AttendanceStatus.Absent
            Case "hd", "halfday" : status = AttendanceStatus.HalfDay
            Case "wo", "weeklyoff" : status = AttendanceStatus.WeeklyOff
            Case "h", "holiday" : status = AttendanceStatus.Holiday
            Case "pl", "paidleave" : status = AttendanceStatus.PaidLeave
            Case Else : Return False
        End Select
        Return True
    End Function
End Class
