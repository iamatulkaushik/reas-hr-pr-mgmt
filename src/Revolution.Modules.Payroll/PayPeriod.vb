''' <summary>Attendance result for one employee for one pay period.</summary>
Public NotInheritable Class PayPeriod
    ''' <param name="periodDays">Calendar days in the period (28 to 31).</param>
    ''' <param name="paidDays">Present, half days (0.5), weekly off, holidays and paid leave.</param>
    ''' <param name="overtimeHours">Approved overtime hours.</param>
    ''' <param name="hoursPerDay">Full-day hours of the shift, for the overtime rate.</param>
    Public Sub New(periodDays As Integer, paidDays As Decimal, overtimeHours As Decimal, hoursPerDay As Decimal)
        Me.PeriodDays = periodDays
        Me.PaidDays = paidDays
        Me.OvertimeHours = overtimeHours
        Me.HoursPerDay = hoursPerDay
    End Sub

    Public ReadOnly Property PeriodDays As Integer
    Public ReadOnly Property PaidDays As Decimal
    Public ReadOnly Property OvertimeHours As Decimal
    Public ReadOnly Property HoursPerDay As Decimal
End Class
