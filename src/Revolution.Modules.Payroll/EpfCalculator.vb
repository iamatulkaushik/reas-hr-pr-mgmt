Imports Revolution.Core

''' <summary>
''' Provident fund for one month. Every rate and ceiling comes from the EpfRate row. Nothing is fixed in code.
''' Employer total is rounded once, then the pension part is taken out, so employee and employer totals always match.
''' </summary>
Public NotInheritable Class EpfCalculator
    Private Sub New()
    End Sub

    ''' <param name="epfWage">Wage flagged for EPF this month, after the wage rule.</param>
    ''' <param name="rate">The rate row in force for the month.</param>
    ''' <param name="contributeOnActualWage">True when the employee contributes above the ceiling by choice.</param>
    Public Shared Function Calculate(epfWage As Decimal, rate As EpfRate, Optional contributeOnActualWage As Boolean = False, Optional pensionOnActualWage As Boolean = False) As EpfResult
        If rate Is Nothing Then
            Throw New ArgumentNullException(NameOf(rate))
        End If
        If epfWage < 0D Then
            Throw New ArgumentOutOfRangeException(NameOf(epfWage), "Wage cannot be negative.")
        End If
        If pensionOnActualWage AndAlso Not contributeOnActualWage Then
            Throw New ArgumentException("Pension on actual wage needs EPF on actual wage.", NameOf(pensionOnActualWage))
        End If
        If epfWage = 0D Then
            Return New EpfResult(0D, 0D, 0D, 0D, 0D, 0D)
        End If
        Dim cappedWage As Decimal = Math.Min(epfWage, rate.WageCeiling)
        Dim pensionBase As Decimal = If(pensionOnActualWage, epfWage, cappedWage)
        Dim epfBase As Decimal = If(contributeOnActualWage, epfWage, cappedWage)
        Dim employee As Decimal = Round(epfBase, rate.EmployeePct)
        Dim eps As Decimal = Round(pensionBase, rate.EmployerEpsPct)
        Dim employerTotal As Decimal = Round(epfBase, rate.EmployerEpfPct + rate.EmployerEpsPct)
        Dim employerEpf As Decimal = Math.Max(0D, employerTotal - eps)
        Dim edli As Decimal = Round(Math.Min(epfWage, rate.EdliWageCeiling), rate.EdliPct)
        Dim admin As Decimal = Round(epfBase, rate.AdminPct)
        Return New EpfResult(epfBase, employee, employerEpf, eps, edli, admin)
    End Function

    Private Shared Function Round(wage As Decimal, pct As Decimal) As Decimal
        Return Rupee.Round(Rupee.Percent(wage, pct), RateRounding.Nearest)
    End Function
End Class
