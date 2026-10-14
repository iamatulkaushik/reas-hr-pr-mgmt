''' <summary>Gratuity rule values for one date. They come from stat.GratuityRule, never from code. The Core type is requested in CR 0013.</summary>
Public NotInheritable Class GratuityTerms
    Public Sub New(minYearsRegular As Integer, minYearsFixedTerm As Integer, daysPerYear As Decimal, monthDivisor As Decimal, roundUpAfterMonths As Integer, capAmount As Decimal)
        Me.MinYearsRegular = minYearsRegular
        Me.MinYearsFixedTerm = minYearsFixedTerm
        Me.DaysPerYear = daysPerYear
        Me.MonthDivisor = monthDivisor
        Me.RoundUpAfterMonths = roundUpAfterMonths
        Me.CapAmount = capAmount
    End Sub

    Public ReadOnly Property MinYearsRegular As Integer
    Public ReadOnly Property MinYearsFixedTerm As Integer
    Public ReadOnly Property DaysPerYear As Decimal
    Public ReadOnly Property MonthDivisor As Decimal
    Public ReadOnly Property RoundUpAfterMonths As Integer
    ''' <summary>Most that can be paid. Zero means no cap.</summary>
    Public ReadOnly Property CapAmount As Decimal
End Class
