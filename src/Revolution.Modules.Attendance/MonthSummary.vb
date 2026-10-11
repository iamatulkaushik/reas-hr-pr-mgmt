''' <summary>Day counts for one employee and month. Days paid plus LOP plus unmarked always equals days in the month.</summary>
Public NotInheritable Class MonthSummary
    Private Const HalfDayValue As Decimal = 0.5D
    Private Const MinutesPerHour As Decimal = 60D

    Friend Sub New(year As Integer, month As Integer, counts As IReadOnlyDictionary(Of AttendanceStatus, Integer), overtimeMinutes As Integer, unmarkedDays As Integer)
        Me.Year = year
        Me.Month = month
        DaysInMonth = DateTime.DaysInMonth(year, month)
        PresentDays = CountOf(counts, AttendanceStatus.Present)
        HalfDays = CountOf(counts, AttendanceStatus.HalfDay)
        WeeklyOffDays = CountOf(counts, AttendanceStatus.WeeklyOff)
        HolidayDays = CountOf(counts, AttendanceStatus.Holiday)
        PaidLeaveDays = CountOf(counts, AttendanceStatus.PaidLeave)
        AbsentDays = CountOf(counts, AttendanceStatus.Absent)
        Me.OvertimeMinutes = overtimeMinutes
        Me.UnmarkedDays = unmarkedDays
    End Sub

    Public ReadOnly Property Year As Integer
    Public ReadOnly Property Month As Integer
    Public ReadOnly Property DaysInMonth As Integer
    Public ReadOnly Property PresentDays As Integer
    Public ReadOnly Property HalfDays As Integer
    Public ReadOnly Property WeeklyOffDays As Integer
    Public ReadOnly Property HolidayDays As Integer
    Public ReadOnly Property PaidLeaveDays As Integer
    Public ReadOnly Property AbsentDays As Integer
    Public ReadOnly Property UnmarkedDays As Integer
    Public ReadOnly Property OvertimeMinutes As Integer

    ''' <summary>Present + weekly off + holiday + paid leave + half of each half day.</summary>
    Public ReadOnly Property DaysPaid As Decimal
        Get
            Return PresentDays + WeeklyOffDays + HolidayDays + PaidLeaveDays + HalfDays * HalfDayValue
        End Get
    End Property

    ''' <summary>Loss of pay: absent days plus half of each half day.</summary>
    Public ReadOnly Property LopDays As Decimal
        Get
            Return AbsentDays + HalfDays * HalfDayValue
        End Get
    End Property

    Public ReadOnly Property OvertimeHours As Decimal
        Get
            Return Math.Round(OvertimeMinutes / MinutesPerHour, 2, MidpointRounding.AwayFromZero)
        End Get
    End Property

    Private Shared Function CountOf(counts As IReadOnlyDictionary(Of AttendanceStatus, Integer), status As AttendanceStatus) As Integer
        Dim value As Integer
        Return If(counts.TryGetValue(status, value), value, 0)
    End Function
End Class
