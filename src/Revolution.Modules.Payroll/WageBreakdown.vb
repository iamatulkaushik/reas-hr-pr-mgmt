''' <summary>Earnings for one employee for one period. All amounts are whole rupees.</summary>
Public NotInheritable Class WageBreakdown
    Public Sub New(lines As IReadOnlyList(Of EarnedLine), overtimeAmount As Decimal, grossEarnings As Decimal, epfWage As Decimal, esiWage As Decimal)
        Me.Lines = lines
        Me.OvertimeAmount = overtimeAmount
        Me.GrossEarnings = grossEarnings
        Me.EpfWage = epfWage
        Me.EsiWage = esiWage
    End Sub

    Public ReadOnly Property Lines As IReadOnlyList(Of EarnedLine)
    Public ReadOnly Property OvertimeAmount As Decimal
    Public ReadOnly Property GrossEarnings As Decimal
    ''' <summary>Wage for EPF after the wage rule, before the ceiling.</summary>
    Public ReadOnly Property EpfWage As Decimal
    ''' <summary>Wage for ESI after the wage rule.</summary>
    Public ReadOnly Property EsiWage As Decimal
End Class
