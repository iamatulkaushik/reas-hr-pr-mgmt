Imports Revolution.Core

''' <summary>
''' The company's leave accrual setting (Owner, 2026-11-01): EarnedDays of leave for every PerWorkedDays days worked, for example 1 for every 20.
''' Rounding settles the fraction (the Factories Act counts half a day or more as one, so Nearest). MaxBalance Nothing means no cap.
''' Set by the company under management settings, one per leave type.
''' </summary>
Public NotInheritable Class LeaveAccrualSetting
    Public Sub New(earnedDays As Decimal, perWorkedDays As Decimal, rounding As RateRounding, Optional maxBalance As Decimal? = Nothing)
        If earnedDays < 0D Then Throw New ArgumentOutOfRangeException(NameOf(earnedDays), "Earned days cannot be negative.")
        If perWorkedDays <= 0D Then Throw New ArgumentOutOfRangeException(NameOf(perWorkedDays), "Worked days must be more than zero.")
        If maxBalance.HasValue AndAlso maxBalance.Value < 0D Then Throw New ArgumentOutOfRangeException(NameOf(maxBalance), "The cap cannot be negative.")
        Me.EarnedDays = earnedDays
        Me.PerWorkedDays = perWorkedDays
        Me.Rounding = rounding
        Me.MaxBalance = maxBalance
    End Sub

    Public ReadOnly Property EarnedDays As Decimal
    Public ReadOnly Property PerWorkedDays As Decimal
    Public ReadOnly Property Rounding As RateRounding
    Public ReadOnly Property MaxBalance As Decimal?
End Class
