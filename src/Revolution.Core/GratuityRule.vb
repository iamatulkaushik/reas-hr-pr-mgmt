''' <summary>
''' The gratuity rule in force on a date (stat.GratuityRule). CapAmount of zero means no cap. Values come from the table, never from code;
''' a missing or unverified row makes IStatutoryRateService.GetGratuityRule fail instead of returning a default (CR 0013).
''' </summary>
Public NotInheritable Class GratuityRule
    Public Sub New(effectiveFrom As Date, minYearsRegular As Integer, minYearsFixedTerm As Integer, daysPerYear As Decimal,
                   monthDivisor As Decimal, roundUpAfterMonths As Integer, capAmount As Decimal,
                   verified As Boolean, verifiedOn As Date?, sourceRef As String)
        Me.EffectiveFrom = effectiveFrom
        Me.MinYearsRegular = minYearsRegular
        Me.MinYearsFixedTerm = minYearsFixedTerm
        Me.DaysPerYear = daysPerYear
        Me.MonthDivisor = monthDivisor
        Me.RoundUpAfterMonths = roundUpAfterMonths
        Me.CapAmount = capAmount
        Me.Verified = verified
        Me.VerifiedOn = verifiedOn
        Me.SourceRef = If(sourceRef, String.Empty)
    End Sub

    Public ReadOnly Property EffectiveFrom As Date
    ''' <summary>Years of service needed for a regular employee.</summary>
    Public ReadOnly Property MinYearsRegular As Integer
    ''' <summary>Years of service needed for a fixed-term employee.</summary>
    Public ReadOnly Property MinYearsFixedTerm As Integer
    ''' <summary>Days of wages paid for each completed year.</summary>
    Public ReadOnly Property DaysPerYear As Decimal
    ''' <summary>Wages for a day are the monthly wage divided by this.</summary>
    Public ReadOnly Property MonthDivisor As Decimal
    ''' <summary>A part year counts as a full year when it is more than this many months.</summary>
    Public ReadOnly Property RoundUpAfterMonths As Integer
    ''' <summary>Most that can be paid. Zero means no cap.</summary>
    Public ReadOnly Property CapAmount As Decimal
    Public ReadOnly Property Verified As Boolean
    Public ReadOnly Property VerifiedOn As Date?
    Public ReadOnly Property SourceRef As String
End Class
