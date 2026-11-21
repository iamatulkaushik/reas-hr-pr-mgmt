Imports Revolution.Core

''' <summary>The columns of the monthly attendance list for the grid and the import (ADR 0026). The month is chosen on screen, not in a column.</summary>
Public NotInheritable Class MonthlyAttendanceTemplate
    Public Const ColCode As String = "Emp Code"
    Public Const ColPresent As String = "Present Days"
    Public Const ColHalf As String = "Half Days"
    Public Const ColWeeklyOff As String = "Weekly Off Days"
    Public Const ColHoliday As String = "Holiday Days"
    Public Const ColPaidLeave As String = "Paid Leave Days"
    Public Const ColAbsent As String = "Absent Days"
    Public Const ColOvertime As String = "OT Hours"
    Public Const ColName As String = "Employee Name"

    Private Sub New()
    End Sub

    Public Shared Function Create() As ImportTemplate
        Return New ImportTemplate("Attendance Monthly", New List(Of ImportColumn) From {
            New ImportColumn(ColCode, True, "Employee code as on the roll.", "E001", {"Employee Code", "Code"}),
            New ImportColumn(ColPresent, True, "Whole days present.", "24", {"Days Present", "Worked Days"}),
            New ImportColumn(ColHalf, False, "Number of half days. Leave empty for none.", "0", {"HD"}),
            New ImportColumn(ColWeeklyOff, False, "Weekly off days. Leave empty to take them from the company's weekly-off rule.", "4", {"WO"}),
            New ImportColumn(ColHoliday, False, "Holidays. Leave empty to take them from the company's holiday list.", "1", {"Holidays", "H"}),
            New ImportColumn(ColPaidLeave, False, "Paid leave taken. Leave empty for none.", "0", {"Leave Days", "PL", "CL EL SL"}),
            New ImportColumn(ColAbsent, False, "Unpaid days. Leave empty for none.", "1", {"Absent", "LOP Days"}),
            New ImportColumn(ColOvertime, False, "Overtime hours in the month, up to 744, two decimals. Leave empty for none.", "0", {"OT", "Overtime", "Overtime Hours"}),
            New ImportColumn(ColName, False, "For your reference. It is never saved.", "Ravi Kumar", {"Name"})})
    End Function
End Class
