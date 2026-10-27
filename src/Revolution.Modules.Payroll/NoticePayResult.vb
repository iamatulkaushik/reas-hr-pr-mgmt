''' <summary>Notice pay recovery at exit. ShortDays is the notice days the employee did not serve. Amount is the recovery in whole rupees.</summary>
Public NotInheritable Class NoticePayResult
    Public Sub New(noticePeriodDays As Integer, servedDays As Integer, shortDays As Integer, dailyWage As Decimal, amount As Decimal)
        Me.NoticePeriodDays = noticePeriodDays
        Me.ServedDays = servedDays
        Me.ShortDays = shortDays
        Me.DailyWage = dailyWage
        Me.Amount = amount
    End Sub

    Public ReadOnly Property NoticePeriodDays As Integer
    Public ReadOnly Property ServedDays As Integer
    Public ReadOnly Property ShortDays As Integer
    Public ReadOnly Property DailyWage As Decimal
    ''' <summary>Whole rupees to recover. Zero when the full notice was served.</summary>
    Public ReadOnly Property Amount As Decimal
End Class
