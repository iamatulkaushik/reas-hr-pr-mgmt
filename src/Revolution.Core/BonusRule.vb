''' <summary>
''' The statutory bonus rule in force on a date (stat.BonusRule, CR 0012). A ceiling of zero means there is no ceiling. Values come from the
''' table, never from code; a missing or unverified row makes IStatutoryRateService.GetBonusRule fail instead of returning a default.
''' </summary>
Public NotInheritable Class BonusRule
    Public Sub New(effectiveFrom As Date, eligibilityWageCeiling As Decimal, calcWageCeiling As Decimal, minPct As Decimal, maxPct As Decimal,
                   minDaysWorked As Integer, verified As Boolean, verifiedOn As Date?, sourceRef As String)
        Me.EffectiveFrom = effectiveFrom
        Me.EligibilityWageCeiling = eligibilityWageCeiling
        Me.CalcWageCeiling = calcWageCeiling
        Me.MinPct = minPct
        Me.MaxPct = maxPct
        Me.MinDaysWorked = minDaysWorked
        Me.Verified = verified
        Me.VerifiedOn = verifiedOn
        Me.SourceRef = If(sourceRef, String.Empty)
    End Sub

    Public ReadOnly Property EffectiveFrom As Date
    ''' <summary>An employee whose monthly wage is above this is not eligible. Zero means no ceiling.</summary>
    Public ReadOnly Property EligibilityWageCeiling As Decimal
    ''' <summary>The monthly wage the bonus is calculated on is capped at this. Zero means no ceiling.</summary>
    Public ReadOnly Property CalcWageCeiling As Decimal
    Public ReadOnly Property MinPct As Decimal
    Public ReadOnly Property MaxPct As Decimal
    ''' <summary>Days the employee must have worked in the accounting year to be eligible.</summary>
    Public ReadOnly Property MinDaysWorked As Integer
    Public ReadOnly Property Verified As Boolean
    Public ReadOnly Property VerifiedOn As Date?
    Public ReadOnly Property SourceRef As String

    ''' <summary>True when the monthly wage is within the eligibility ceiling (always true when there is no ceiling).</summary>
    Public Function IsEligibleOnWage(monthlyWage As Decimal) As Boolean
        Return EligibilityWageCeiling = 0D OrElse monthlyWage <= EligibilityWageCeiling
    End Function

    ''' <summary>The monthly wage to calculate bonus on: the wage itself, or the calculation ceiling when the wage is higher.</summary>
    Public Function CalculationWage(monthlyWage As Decimal) As Decimal
        Return If(CalcWageCeiling > 0D AndAlso monthlyWage > CalcWageCeiling, CalcWageCeiling, monthlyWage)
    End Function
End Class
