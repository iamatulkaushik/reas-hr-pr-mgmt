''' <summary>Company pay rules the wage builder needs. Values come from pay.CompanyPolicy.</summary>
Public NotInheritable Class PayPolicy
    Public Sub New(otMultiplier As Decimal, otInEpfWage As Boolean, otInEsiWage As Boolean, dailyWageDivisor As Decimal)
        Me.OtMultiplier = otMultiplier
        Me.OtInEpfWage = otInEpfWage
        Me.OtInEsiWage = otInEsiWage
        Me.DailyWageDivisor = dailyWageDivisor
    End Sub

    Public ReadOnly Property OtMultiplier As Decimal
    Public ReadOnly Property OtInEpfWage As Boolean
    Public ReadOnly Property OtInEsiWage As Boolean
    Public ReadOnly Property DailyWageDivisor As Decimal
End Class
