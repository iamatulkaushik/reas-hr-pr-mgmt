Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' One employee's pay for one run period. Wage builder, then EPF (members only), ESI (members only), professional tax (employees it applies to), then net pay with
''' the loan EMI cap. A missing salary, an unmarked day, a missing shift (when overtime is claimed) or a professional tax slab gap fails the employee with a message
''' that starts with the employee code. The run never guesses and never saves a partial result.
''' ESI daily wage for the exemption is the monthly salary over the period days (monthly employee) or over the divisor (daily wage).
''' </summary>
Public NotInheritable Class PayrollRunCalculator
    Private Const NoShiftHours As Decimal = 8D

    Private Sub New()
    End Sub

    ''' <summary>
    ''' tdsForGross is called once the wage is built and gives the income tax due this month, or a failure that fails the employee. Nothing means no tax (plan without TDS).
    ''' </summary>
    Public Shared Function Calculate(facts As RunEmployeeFacts, period As RunPeriod, policy As PolicySettings, rates As RunRates,
                                     Optional tdsForGross As Func(Of WageBreakdown, OperationResult(Of Decimal)) = Nothing) As OperationResult(Of RunLineResult)
        If facts Is Nothing Then Throw New ArgumentNullException(NameOf(facts))
        If period Is Nothing Then Throw New ArgumentNullException(NameOf(period))
        If policy Is Nothing Then Throw New ArgumentNullException(NameOf(policy))
        If rates Is Nothing Then Throw New ArgumentNullException(NameOf(rates))

        If facts.Salary.Count = 0 Then Return Fail(facts, "no salary on file.")
        If facts.UnmarkedDays > 0 Then Return Fail(facts, $"{facts.UnmarkedDays.ToString(CultureInfo.InvariantCulture)} day(s) have no attendance mark.")
        If facts.PaidDays < 0D Then Return Fail(facts, "paid days are negative.")
        If Not facts.IsDailyWage AndAlso facts.PaidDays > period.Days Then Return Fail(facts, "paid days are more than the days in the period.")
        If facts.OvertimeHours > 0D AndAlso Not facts.HoursPerDay.HasValue Then Return Fail(facts, "overtime is marked but no shift is assigned.")

        Dim hours As Decimal = If(facts.HoursPerDay.HasValue, facts.HoursPerDay.Value, NoShiftHours)
        Dim payPeriod As New PayPeriod(period.Days, facts.PaidDays, facts.OvertimeHours, hours)
        Dim lines As List(Of SalaryLineInput) = facts.Salary.Select(Function(l) l.ToInput()).ToList()
        Dim wage As WageBreakdown = WageBuilder.Build(lines, facts.IsDailyWage, payPeriod, policy.ToPayPolicy(), rates.WageRule)

        Dim epf As EpfResult = If(facts.IsEpfMember,
                                  EpfCalculator.Calculate(wage.EpfWage, rates.Epf, facts.EpfOnHigherWage, facts.EpsOnHigherWage),
                                  New EpfResult(0D, 0D, 0D, 0D, 0D, 0D))

        Dim esi As EsiResult = New EsiResult(False, 0D, 0D)
        If facts.IsEsiMember Then
            Dim monthly As Decimal = facts.Salary.Sum(Function(l) l.Amount)
            Dim dailyWage As Decimal = monthly / If(facts.IsDailyWage, policy.DailyWageDivisor, CDec(period.Days))
            esi = EsiCalculator.Calculate(wage.EsiWage, dailyWage, rates.Esi, facts.EsiStayEligible)
        End If

        Dim professionalTax As Decimal = 0D
        If facts.PtStateId.HasValue Then
            Dim rule As PtRule = Nothing
            If Not rates.PtRules.TryGetValue(facts.PtStateId.Value, rule) Then Return Fail(facts, "no professional tax rule for the state of the factory.")
            Dim pt As OperationResult(Of Decimal) = PtCalculator.Calculate(rule, facts.GenderId, wage.GrossEarnings, period.WageMonth)
            If Not pt.Succeeded Then Return Fail(facts, pt.Message)
            professionalTax = pt.Value
        End If

        Dim tdsDue As Decimal = 0D
        If tdsForGross IsNot Nothing Then
            Dim tax As OperationResult(Of Decimal) = tdsForGross(wage)
            If Not tax.Succeeded Then Return Fail(facts, tax.Message)
            tdsDue = tax.Value
        End If

        Dim statutory As Decimal = epf.EmployeeShare + esi.EmployeeShare + professionalTax
        Dim scheduledEmi As Decimal = LoanRecoveryAllocator.ScheduledEmi(facts.Installments, period.MonthKey)
        Dim net As NetPayResult = NetPayCalculator.Calculate(wage.GrossEarnings, statutory, facts.OtherDeductions, facts.CarryForwardIn, scheduledEmi, policy.EmiCapPct, tdsDue)

        Dim componentByCode As New Dictionary(Of String, Integer)(StringComparer.Ordinal)
        For Each l As SalaryLineView In facts.Salary
            componentByCode(l.Code) = l.ComponentId
        Next
        Dim earnings As List(Of EarningAmount) = wage.Lines.Select(Function(e) New EarningAmount(componentByCode(e.Code), e.Amount)).ToList()

        Dim result As New RunLineResult(facts.EmployeeId, facts.FactoryId, facts.PaidDays, facts.OvertimeHours, wage.OvertimeAmount, wage.GrossEarnings,
                                        wage.EpfWage, wage.EsiWage, epf.EmployeeShare, epf.EmployerEpf, epf.EmployerEps, epf.Edli, epf.Admin,
                                        esi.EmployeeShare, esi.EmployerShare, professionalTax, net.EmiRecovered, net.EmiDeferred, facts.OtherDeductions,
                                        facts.CarryForwardIn, net.CarryForwardOut, wage.GrossEarnings - net.NetPay, net.NetPay, earnings,
                                        net.TdsTaken, net.TdsShortfall)
        Return OperationResult(Of RunLineResult).Ok(result)
    End Function

    Private Shared Function Fail(facts As RunEmployeeFacts, reason As String) As OperationResult(Of RunLineResult)
        Return OperationResult(Of RunLineResult).Fail($"{facts.EmpCode}: {reason}")
    End Function
End Class
