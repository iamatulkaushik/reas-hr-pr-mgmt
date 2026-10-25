''' <summary>The dates of a payroll run. Days counts both ends. Wage month and month key follow the end date.</summary>
Public NotInheritable Class RunPeriod
    Public Sub New(periodFrom As Date, periodTo As Date)
        If periodTo < periodFrom Then Throw New ArgumentException("Period end is before its start.", NameOf(periodTo))
        Me.PeriodFrom = periodFrom.Date
        Me.PeriodTo = periodTo.Date
    End Sub

    Public ReadOnly Property PeriodFrom As Date
    Public ReadOnly Property PeriodTo As Date
    Public ReadOnly Property Days As Integer
        Get
            Return CInt((PeriodTo - PeriodFrom).TotalDays) + 1
        End Get
    End Property

    ''' <summary>Month of the end date, 1 to 12. Professional tax and loans follow it.</summary>
    Public ReadOnly Property WageMonth As Integer
        Get
            Return PeriodTo.Month
        End Get
    End Property

    ''' <summary>Year and month of the end date as yyyymm, the key loan installments use.</summary>
    Public ReadOnly Property MonthKey As Integer
        Get
            Return PeriodTo.Year * 100 + PeriodTo.Month
        End Get
    End Property

    ''' <summary>The date the statutory rate rows are read for. Open question for the CA when a rate changes inside the period; one place to change it.</summary>
    Public ReadOnly Property RateDate As Date
        Get
            Return PeriodTo
        End Get
    End Property
End Class
