Imports Revolution.Core

''' <summary>The projection for one employee in one run: the year's tax and the tax to deduct this month.</summary>
Public NotInheritable Class TdsProjectionResult
    Public Sub New(regime As TdsRegime, usedDefaultRegime As Boolean, projectedIncome As Decimal, hraExempt As Decimal, annual As TdsAnnualResult,
                   taxDeductedBefore As Decimal, remainingMonths As Integer, tdsThisMonth As Decimal)
        Me.Regime = regime
        Me.UsedDefaultRegime = usedDefaultRegime
        Me.ProjectedIncome = projectedIncome
        Me.HraExempt = hraExempt
        Me.Annual = annual
        Me.TaxDeductedBefore = taxDeductedBefore
        Me.RemainingMonths = remainingMonths
        Me.TdsThisMonth = tdsThisMonth
    End Sub

    Public ReadOnly Property Regime As TdsRegime
    ''' <summary>True when the employee had no declaration and the new regime was taken by default.</summary>
    Public ReadOnly Property UsedDefaultRegime As Boolean
    ''' <summary>Projected salary for the year plus previous employer salary and perquisites, before the HRA exemption.</summary>
    Public ReadOnly Property ProjectedIncome As Decimal
    Public ReadOnly Property HraExempt As Decimal
    Public ReadOnly Property Annual As TdsAnnualResult
    ''' <summary>TDS taken before this month by this employer and the previous one.</summary>
    Public ReadOnly Property TaxDeductedBefore As Decimal
    Public ReadOnly Property RemainingMonths As Integer
    Public ReadOnly Property TdsThisMonth As Decimal
End Class
