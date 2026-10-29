Imports Revolution.Core

''' <summary>
''' Net pay for one employee for one period.
''' Pay before EMI is gross less statutory deductions, other deductions and any shortfall carried in. If it is below zero, net is zero,
''' the shortfall carries forward, and the whole EMI is deferred. Otherwise the EMI taken is the smaller of the scheduled EMI and the company cap
''' (a share of pay before EMI, rounded down so the cap is never passed). The rest of the EMI is deferred.
''' Income tax (CR 0019) is a statutory deduction: it is taken before the EMI and never deferred, but limited to what is left of gross after the other statutory deductions, other
''' deductions and the shortfall carried in, so tax alone never drives net below zero. The part not taken is reported as TdsShortfall.
''' </summary>
Public NotInheritable Class NetPayCalculator
    Private Sub New()
    End Sub

    Public Shared Function Calculate(gross As Decimal, statutoryDeductions As Decimal, otherDeductions As Decimal, carryForwardIn As Decimal, scheduledEmi As Decimal, emiCapPct As Decimal) As NetPayResult
        Return Calculate(gross, statutoryDeductions, otherDeductions, carryForwardIn, scheduledEmi, emiCapPct, 0D)
    End Function

    Public Shared Function Calculate(gross As Decimal, statutoryDeductions As Decimal, otherDeductions As Decimal, carryForwardIn As Decimal, scheduledEmi As Decimal, emiCapPct As Decimal,
                                     tdsDue As Decimal) As NetPayResult
        If tdsDue < 0D Then Throw New ArgumentOutOfRangeException(NameOf(tdsDue), "Amounts cannot be negative.")
        If gross < 0D OrElse statutoryDeductions < 0D OrElse otherDeductions < 0D OrElse carryForwardIn < 0D OrElse scheduledEmi < 0D Then
            Throw New ArgumentOutOfRangeException(NameOf(gross), "Amounts cannot be negative.")
        End If
        If emiCapPct < 0D OrElse emiCapPct > 100D Then
            Throw New ArgumentOutOfRangeException(NameOf(emiCapPct), "EMI cap must be between 0 and 100.")
        End If
        Dim room As Decimal = gross - statutoryDeductions - otherDeductions - carryForwardIn
        Dim tdsTaken As Decimal = Math.Min(tdsDue, Math.Max(0D, room))
        Dim before As Decimal = room - tdsTaken
        If before < 0D Then
            Return New NetPayResult(0D, 0D, scheduledEmi, -before, tdsTaken, tdsDue - tdsTaken)
        End If
        Dim limit As Decimal = Rupee.Round(Rupee.Percent(before, emiCapPct), RateRounding.Down)
        Dim taken As Decimal = Math.Min(scheduledEmi, limit)
        Return New NetPayResult(Rupee.Round(before - taken, RateRounding.Nearest), taken, scheduledEmi - taken, 0D, tdsTaken, tdsDue - tdsTaken)
    End Function
End Class
