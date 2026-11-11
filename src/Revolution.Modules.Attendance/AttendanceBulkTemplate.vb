Imports Revolution.Core

''' <summary>The columns of the attendance list for Multiple and Import (ADR 0025).</summary>
Public NotInheritable Class AttendanceBulkTemplate
    Public Const ColCode As String = "Emp Code"
    Public Const ColDate As String = "Date"
    Public Const ColStatus As String = "Status"
    Public Const ColOvertime As String = "OT Hours"

    Private Sub New()
    End Sub

    Public Shared Function Create() As ImportTemplate
        Return New ImportTemplate("Attendance", New List(Of ImportColumn) From {
            New ImportColumn(ColCode, True, "Employee code as on the roll.", "E001", {"Employee Code", "EmpCode", "Code"}),
            New ImportColumn(ColDate, True, "Day to mark, as dd-mm-yyyy or yyyy-mm-dd. Not in the future.", "2026-11-03", {"Work Date", "Day"}),
            New ImportColumn(ColStatus, True, "P, A, HD, WO, H or PL (Present, Absent, Half day, Weekly off, Holiday, Paid leave).", "P", {"Attendance"}),
            New ImportColumn(ColOvertime, False, "Overtime hours on a Present day, up to 24. Leave empty for none.", "0", {"OT", "Overtime", "Overtime Hours"})})
    End Function
End Class
