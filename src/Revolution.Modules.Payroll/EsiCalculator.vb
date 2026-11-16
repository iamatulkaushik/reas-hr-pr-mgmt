Imports Revolution.Core

''' <summary>
''' State insurance for one month. Ceiling, percentages and rounding come from the EsiRate row.
''' stayEligible carries the contribution-period rule: an employee who was eligible at the start of the period stays in until its end.
''' higherCeiling is the employee's disability flag: the row's disability ceiling replaces the normal one when the row has it (CR 0030).
''' </summary>
Public NotInheritable Class EsiCalculator
    Private Sub New()
    End Sub

    Public Shared Function Calculate(esiWage As Decimal, dailyWage As Decimal, rate As EsiRate, stayEligible As Boolean, Optional higherCeiling As Boolean = False) As EsiResult
        If rate Is Nothing Then
            Throw New ArgumentNullException(NameOf(rate))
        End If
        If esiWage < 0D Then
            Throw New ArgumentOutOfRangeException(NameOf(esiWage), "Wage cannot be negative.")
        End If
        If dailyWage < 0D Then
            Throw New ArgumentOutOfRangeException(NameOf(dailyWage), "Daily wage cannot be negative.")
        End If
        Dim eligible As Boolean = esiWage > 0D AndAlso (stayEligible OrElse esiWage <= rate.CeilingFor(higherCeiling))
        If Not eligible Then
            Return New EsiResult(False, 0D, 0D)
        End If
        Dim exempt As Boolean = rate.DailyWageExemption > 0D AndAlso dailyWage <= rate.DailyWageExemption
        Dim employee As Decimal = If(exempt, 0D, Rupee.Round(Rupee.Percent(esiWage, rate.EmployeePct), rate.Rounding))
        Dim employer As Decimal = Rupee.Round(Rupee.Percent(esiWage, rate.EmployerPct), rate.Rounding)
        Return New EsiResult(True, employee, employer)
    End Function
End Class
