Imports Revolution.Core
Imports Revolution.Modules.Payroll

''' <summary>
''' Puts one wage month's arrears input together: what the approved run paid (its stored line) against what the employee should have been paid
''' (the same month worked again with the salary in force now, by the regular run calculator, without tax or loans). Membership of EPF and ESI,
''' the higher wage options and the rates of the wage month come from the facts and rates passed in (Owner, 2026-11-01). Pure.
''' </summary>
Public NotInheritable Class ArrearsMonthPreparer
    Private Sub New()
    End Sub

    Public Shared Function Prepare(paid As RunLineResult, dueFacts As RunEmployeeFacts, period As RunPeriod, policy As PolicySettings, rates As RunRates) As OperationResult(Of ArrearsMonthInput)
        If paid Is Nothing Then Throw New ArgumentNullException(NameOf(paid))
        If dueFacts Is Nothing Then Throw New ArgumentNullException(NameOf(dueFacts))
        If period Is Nothing Then Throw New ArgumentNullException(NameOf(period))
        If policy Is Nothing Then Throw New ArgumentNullException(NameOf(policy))
        If rates Is Nothing Then Throw New ArgumentNullException(NameOf(rates))
        If paid.EmployeeId <> dueFacts.EmployeeId Then Return OperationResult(Of ArrearsMonthInput).Fail("The paid line and the salary facts are for different employees.")
        Dim due As OperationResult(Of RunLineResult) = PayrollRunCalculator.Calculate(dueFacts, period, policy, rates)
        If Not due.Succeeded Then Return OperationResult(Of ArrearsMonthInput).Fail(due.Message)
        Dim codes As New Dictionary(Of Integer, String)()
        For Each l As SalaryLineView In dueFacts.Salary
            codes(l.ComponentId) = l.Code
        Next
        Dim ids As New Dictionary(Of String, Integer)()
        For Each pair As KeyValuePair(Of Integer, String) In codes
            ids(pair.Value) = pair.Key
        Next
        For Each e As EarningAmount In paid.Earnings.Concat(due.Value.Earnings)
            If Not codes.ContainsKey(e.ComponentId) Then ids("C" & e.ComponentId.ToString(Globalization.CultureInfo.InvariantCulture)) = e.ComponentId
        Next
        Dim monthly As Decimal = dueFacts.Salary.Sum(Function(l As SalaryLineView) l.Amount)
        Dim dailyWage As Decimal = monthly / If(dueFacts.IsDailyWage, policy.DailyWageDivisor, CDec(period.Days))
        Dim month As New Date(period.PeriodTo.Year, period.PeriodTo.Month, 1)
        Return OperationResult(Of ArrearsMonthInput).Ok(New ArrearsMonthInput(month, Breakdown(paid, codes), Breakdown(due.Value, codes), rates, dailyWage,
                                                                              dueFacts.EsiStayEligible, dueFacts.EpfOnHigherWage, dueFacts.EpsOnHigherWage,
                                                                              dueFacts.IsEpfMember, dueFacts.IsEsiMember, ids))
    End Function

    Private Shared Function Breakdown(line As RunLineResult, codes As Dictionary(Of Integer, String)) As WageBreakdown
        Dim earned As New List(Of EarnedLine)()
        For Each e As EarningAmount In line.Earnings
            Dim code As String = Nothing
            If Not codes.TryGetValue(e.ComponentId, code) Then code = "C" & e.ComponentId.ToString(Globalization.CultureInfo.InvariantCulture)
            earned.Add(New EarnedLine(code, e.Amount))
        Next
        Return New WageBreakdown(earned, line.OvertimeAmount, line.GrossEarnings, line.EpfWage, line.EsiWage)
    End Function
End Class
