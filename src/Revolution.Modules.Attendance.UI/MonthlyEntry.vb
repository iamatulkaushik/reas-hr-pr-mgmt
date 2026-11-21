Imports System.Globalization
Imports System.Windows.Forms
Imports Revolution.Modules.Attendance

''' <summary>
''' Builds the monthly attendance list screens (ADR 0026): Multiple and Update open the list with every active employee of the company and the saved values;
''' Import opens the file chooser. Both use BulkEntryForm (ADR 0025) with the "Attendance Monthly" template. The month is asked first and defaults to the month before.
''' </summary>
Friend NotInheritable Class MonthlyEntry
    Private Const MonthFormat As String = "MMMM yyyy"
    Private Const ShownCodes As Integer = 5

    Private Sub New()
    End Sub

    Public Shared Function CreateList(services As AttendanceServices, importer As ITableImporter, exporter As ITableExporter, userName As String, startWithFile As Boolean) As Form
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        Dim period = MonthlyPeriodPicker.Pick(services.Monthly.DefaultPeriod())
        Dim handler = services.Monthly.CreateBulkHandler(period)
        Dim title = $"{If(startWithFile, "Import attendance", "Monthly attendance")} - {period.ToString(MonthFormat, CultureInfo.InvariantCulture)}"
        Dim initial As IReadOnlyList(Of ImportRow) = If(startWithFile, Nothing, RowsFor(services, period))
        Return New BulkEntryForm(title, AttendanceMonthlyService.BulkTemplate(), handler, importer, exporter, startWithFile, userName, initial,
                                 Function() MissingText(handler))
    End Function

    ''' <summary>One row per active employee employed in the month: the saved counts, or empty cells for an employee with no row yet. Nothing when the month cannot be read.</summary>
    Private Shared Function RowsFor(services As AttendanceServices, period As Date) As IReadOnlyList(Of ImportRow)
        Dim loaded = services.Monthly.LoadMonth(period)
        If Not loaded.Succeeded OrElse loaded.Value.Count = 0 Then Return Nothing
        Dim rows As New List(Of ImportRow)()
        For Each person In loaded.Value
            Dim values As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase) From {
                {MonthlyAttendanceTemplate.ColCode, person.EmpCode},
                {MonthlyAttendanceTemplate.ColName, person.EmployeeName}}
            If person.HasRow Then
                values(MonthlyAttendanceTemplate.ColPresent) = Text(person.PresentDays)
                values(MonthlyAttendanceTemplate.ColHalf) = Text(person.HalfDays)
                values(MonthlyAttendanceTemplate.ColWeeklyOff) = Text(person.WeeklyOffDays)
                values(MonthlyAttendanceTemplate.ColHoliday) = Text(person.HolidayDays)
                values(MonthlyAttendanceTemplate.ColPaidLeave) = Text(person.PaidLeaveDays)
                values(MonthlyAttendanceTemplate.ColAbsent) = Text(person.AbsentDays)
                values(MonthlyAttendanceTemplate.ColOvertime) = person.OvertimeHours.ToString("0.##", CultureInfo.InvariantCulture)
            End If
            rows.Add(New ImportRow(rows.Count + 1, values))
        Next
        Return rows
    End Function

    Private Shared Function Text(value As Integer) As String
        Return value.ToString(CultureInfo.InvariantCulture)
    End Function

    ''' <summary>The employees the sheet did not mention: a missing employee blocks the month close.</summary>
    Private Shared Function MissingText(handler As MonthlyAttendanceHandler) As String
        Dim total As Integer
        Dim codes = handler.MissingFromSheet(ShownCodes, total)
        If total = 0 Then Return String.Empty
        Dim more = If(total > codes.Count, " and more", String.Empty)
        Return $"{total} employee(s) are not in the list ({String.Join(", ", codes)}{more}). A missing employee blocks the month close."
    End Function
End Class
