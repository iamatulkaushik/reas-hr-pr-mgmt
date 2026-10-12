''' <summary>State insurance rates in force on a date. Percentages are as stored (0.75 means 0.75 percent).</summary>
Public NotInheritable Class EsiRate
    Public Sub New(effectiveFrom As Date, employeePct As Decimal, employerPct As Decimal, wageCeiling As Decimal, dailyWageExemption As Decimal,
                   rounding As RateRounding, verified As Boolean, sourceRef As String)
        Me.EffectiveFrom = effectiveFrom
        Me.EmployeePct = employeePct
        Me.EmployerPct = employerPct
        Me.WageCeiling = wageCeiling
        Me.DailyWageExemption = dailyWageExemption
        Me.Rounding = rounding
        Me.Verified = verified
        Me.SourceRef = If(sourceRef, String.Empty)
    End Sub

    Public ReadOnly Property EffectiveFrom As Date
    Public ReadOnly Property EmployeePct As Decimal
    Public ReadOnly Property EmployerPct As Decimal
    Public ReadOnly Property WageCeiling As Decimal
    ''' <summary>Employees earning up to this much a day do not pay the employee share. Zero means no exemption.</summary>
    Public ReadOnly Property DailyWageExemption As Decimal
    Public ReadOnly Property Rounding As RateRounding
    Public ReadOnly Property Verified As Boolean
    Public ReadOnly Property SourceRef As String
End Class
