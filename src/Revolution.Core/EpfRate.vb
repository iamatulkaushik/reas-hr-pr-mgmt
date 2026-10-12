''' <summary>Provident fund rates in force on a date. Percentages are as stored (12 means 12 percent). Read through IStatutoryRateService.</summary>
Public NotInheritable Class EpfRate
    Public Sub New(effectiveFrom As Date, employeePct As Decimal, employerEpfPct As Decimal, employerEpsPct As Decimal, edliPct As Decimal,
                   adminPct As Decimal, adminMinimum As Decimal, wageCeiling As Decimal, edliWageCeiling As Decimal, verified As Boolean, sourceRef As String)
        Me.EffectiveFrom = effectiveFrom
        Me.EmployeePct = employeePct
        Me.EmployerEpfPct = employerEpfPct
        Me.EmployerEpsPct = employerEpsPct
        Me.EdliPct = edliPct
        Me.AdminPct = adminPct
        Me.AdminMinimum = adminMinimum
        Me.WageCeiling = wageCeiling
        Me.EdliWageCeiling = edliWageCeiling
        Me.Verified = verified
        Me.SourceRef = If(sourceRef, String.Empty)
    End Sub

    Public ReadOnly Property EffectiveFrom As Date
    Public ReadOnly Property EmployeePct As Decimal
    Public ReadOnly Property EmployerEpfPct As Decimal
    Public ReadOnly Property EmployerEpsPct As Decimal
    Public ReadOnly Property EdliPct As Decimal
    Public ReadOnly Property AdminPct As Decimal
    Public ReadOnly Property AdminMinimum As Decimal
    Public ReadOnly Property WageCeiling As Decimal
    Public ReadOnly Property EdliWageCeiling As Decimal
    ''' <summary>True once the CA has signed this row off.</summary>
    Public ReadOnly Property Verified As Boolean
    Public ReadOnly Property SourceRef As String
End Class
