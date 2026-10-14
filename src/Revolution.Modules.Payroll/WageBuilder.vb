Imports Revolution.Core

''' <summary>
''' Turns a salary and one period's attendance into earnings and statutory wages.
''' Monthly employee: each head is the monthly figure times paid days over period days (calendar days).
''' Daily-wage employee: each head is the monthly figure over the divisor (26) times paid days.
''' Overtime: ordinary rate is basic, DA and heads marked as overtime base, per day then per hour, times the multiplier.
''' Each head and the overtime are rounded once, to the nearest rupee.
''' </summary>
Public NotInheritable Class WageBuilder
    Private Sub New()
    End Sub

    Public Shared Function Build(lines As IReadOnlyList(Of SalaryLineInput), isDailyWage As Boolean, period As PayPeriod, policy As PayPolicy, wageRule As WageRule) As WageBreakdown
        If lines Is Nothing Then Throw New ArgumentNullException(NameOf(lines))
        If period Is Nothing Then Throw New ArgumentNullException(NameOf(period))
        If policy Is Nothing Then Throw New ArgumentNullException(NameOf(policy))
        If wageRule Is Nothing Then Throw New ArgumentNullException(NameOf(wageRule))
        Validate(period, policy, isDailyWage)

        Dim earned As New List(Of EarnedLine)
        Dim baseEarned As Decimal = 0D
        Dim otherEarned As Decimal = 0D
        Dim ordinaryMonthly As Decimal = 0D
        For Each l As SalaryLineInput In lines
            If l.Amount < 0D Then Throw New ArgumentOutOfRangeException(NameOf(lines), "A salary amount cannot be negative.")
            Dim amount As Decimal = Rupee.Round(PeriodShare(l.Amount, isDailyWage, period, policy), RateRounding.Nearest)
            earned.Add(New EarnedLine(l.Code, amount))
            If l.IsStatutoryBase Then baseEarned += amount Else otherEarned += amount
            If l.IsStatutoryBase OrElse l.IsOtBase Then ordinaryMonthly += l.Amount
        Next

        Dim ordinaryDaily As Decimal = ordinaryMonthly / If(isDailyWage, policy.DailyWageDivisor, CDec(period.PeriodDays))
        Dim overtime As Decimal = Rupee.Round(ordinaryDaily / period.HoursPerDay * period.OvertimeHours * policy.OtMultiplier, RateRounding.Nearest)
        Dim gross As Decimal = baseEarned + otherEarned + overtime

        Dim epfWage As Decimal = StatutoryWage.Compute(baseEarned + If(policy.OtInEpfWage, overtime, 0D), gross, otherEarned + If(policy.OtInEpfWage, 0D, overtime), wageRule)
        Dim esiWage As Decimal = StatutoryWage.Compute(baseEarned + If(policy.OtInEsiWage, overtime, 0D), gross, otherEarned + If(policy.OtInEsiWage, 0D, overtime), wageRule)
        Return New WageBreakdown(earned, overtime, gross, epfWage, esiWage)
    End Function

    Private Shared Function PeriodShare(monthly As Decimal, isDailyWage As Boolean, period As PayPeriod, policy As PayPolicy) As Decimal
        If isDailyWage Then
            Return monthly / policy.DailyWageDivisor * period.PaidDays
        End If
        Return monthly * period.PaidDays / period.PeriodDays
    End Function

    Private Shared Sub Validate(period As PayPeriod, policy As PayPolicy, isDailyWage As Boolean)
        If period.PeriodDays < 1 OrElse period.PeriodDays > 31 Then Throw New ArgumentOutOfRangeException(NameOf(period), "Period days must be 1 to 31.")
        If period.PaidDays < 0D Then Throw New ArgumentOutOfRangeException(NameOf(period), "Paid days cannot be negative.")
        If Not isDailyWage AndAlso period.PaidDays > period.PeriodDays Then Throw New ArgumentOutOfRangeException(NameOf(period), "Paid days cannot exceed period days.")
        If period.OvertimeHours < 0D Then Throw New ArgumentOutOfRangeException(NameOf(period), "Overtime hours cannot be negative.")
        If period.HoursPerDay <= 0D Then Throw New ArgumentOutOfRangeException(NameOf(period), "Hours per day must be above zero.")
        If policy.OtMultiplier < 1D Then Throw New ArgumentOutOfRangeException(NameOf(policy), "Overtime multiplier cannot be below 1.")
        If policy.DailyWageDivisor <= 0D Then Throw New ArgumentOutOfRangeException(NameOf(policy), "Divisor must be above zero.")
    End Sub
End Class
