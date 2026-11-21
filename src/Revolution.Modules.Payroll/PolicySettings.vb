''' <summary>Company pay rules as stored in pay.CompanyPolicy. PayCycleStartDay 1 is the calendar month, 26 is the 26th to 25th cycle (Owner).</summary>
Public NotInheritable Class PolicySettings
    Public Const DefaultOtHoursPerDay As Decimal = 8D

    Public Sub New(payCycleStartDay As Integer, otMultiplier As Decimal, otInEpfWage As Boolean, otInEsiWage As Boolean,
                   dailyWageDivisor As Decimal, emiCapPct As Decimal, Optional otHoursPerDay As Decimal = DefaultOtHoursPerDay)
        Me.PayCycleStartDay = payCycleStartDay
        Me.OtMultiplier = otMultiplier
        Me.OtInEpfWage = otInEpfWage
        Me.OtInEsiWage = otInEsiWage
        Me.DailyWageDivisor = dailyWageDivisor
        Me.EmiCapPct = emiCapPct
        Me.OtHoursPerDay = otHoursPerDay
    End Sub

    Public ReadOnly Property PayCycleStartDay As Integer
    Public ReadOnly Property OtMultiplier As Decimal
    Public ReadOnly Property OtInEpfWage As Boolean
    Public ReadOnly Property OtInEsiWage As Boolean
    Public ReadOnly Property DailyWageDivisor As Decimal
    ''' <summary>Most of net pay (percent) a loan EMI may take. Owner: 25.</summary>
    Public ReadOnly Property EmiCapPct As Decimal
    ''' <summary>Hours of a normal day that monthly overtime hours are converted with when an employee has no shift (ADR 0026). Default 8.</summary>
    Public ReadOnly Property OtHoursPerDay As Decimal

    ''' <summary>The part the wage builder reads.</summary>
    Public Function ToPayPolicy() As PayPolicy
        Return New PayPolicy(OtMultiplier, OtInEpfWage, OtInEsiWage, DailyWageDivisor)
    End Function
End Class
