Imports Revolution.Core

''' <summary>
''' Works out the income tax to take in one run (CR 0019). The twelve months of the financial year, April to March, are filled like this: a month already in locked pay
''' takes its figures from there; the run's own month takes this pay (plus any locked arrears pay in the same month); every month after it repeats this pay. A month before the
''' employee's first locked pay is zero. Basic plus DA and HRA of this pay are the salary structure's shares scaled by earned over structure pay, so a part month stays in proportion.
''' The projection is TdsProjectionService.Project, passed in so this stays pure; its result gives the amount for this month.
''' </summary>
Public NotInheritable Class RunTdsPlanner
    Private Sub New()
    End Sub

    Public Shared Function Plan(period As RunPeriod, wage As WageBreakdown, salary As IReadOnlyList(Of SalaryLineView), tax As EmployeeTaxFacts,
                                project As Func(Of TdsProjectionInput, OperationResult(Of TdsProjectionResult))) As OperationResult(Of Decimal)
        If period Is Nothing Then Throw New ArgumentNullException(NameOf(period))
        If wage Is Nothing Then Throw New ArgumentNullException(NameOf(wage))
        If tax Is Nothing Then Throw New ArgumentNullException(NameOf(tax))
        If project Is Nothing Then Throw New ArgumentNullException(NameOf(project))
        Dim financialYear As Integer = FinancialYearCalendar.StartYear(period.PeriodTo)
        Dim months As IReadOnlyList(Of TdsMonthFacts) = BuildMonths(financialYear, period.MonthKey, ThisPay(wage, salary, period.MonthKey), tax)
        Dim input As New TdsProjectionInput(tax.EmployeeId, financialYear, tax.DateOfBirth, period.MonthKey, months, tax.TaxDeductedBefore, tax.Declaration)
        Dim result As OperationResult(Of TdsProjectionResult) = project(input)
        If Not result.Succeeded Then Return OperationResult(Of Decimal).Fail(result.Message)
        Return OperationResult(Of Decimal).Ok(result.Value.TdsThisMonth)
    End Function

    Friend Shared Function ThisPay(wage As WageBreakdown, salary As IReadOnlyList(Of SalaryLineView), monthKey As Integer) As TdsMonthFacts
        Dim structurePay As Decimal = 0D
        Dim basicDa As Decimal = 0D
        Dim hra As Decimal = 0D
        For Each line As SalaryLineView In salary
            structurePay += line.Amount
            If line.IsBasic OrElse line.IsDa Then basicDa += line.Amount
            If line.IsHra Then hra += line.Amount
        Next
        Dim share As Decimal = If(structurePay > 0D, wage.GrossEarnings / structurePay, 0D)
        Return New TdsMonthFacts(monthKey, basicDa * share, hra * share, wage.GrossEarnings)
    End Function

    Private Shared Function BuildMonths(financialYear As Integer, runMonthKey As Integer, thisPay As TdsMonthFacts, tax As EmployeeTaxFacts) As IReadOnlyList(Of TdsMonthFacts)
        Dim locked As New Dictionary(Of Integer, TdsMonthFacts)()
        For Each month As TdsMonthFacts In tax.LockedMonths
            locked(month.MonthKey) = month
        Next
        Dim first As Integer = financialYear * 100 + 4
        Dim months As New List(Of TdsMonthFacts)(12)
        For index As Integer = 0 To 11
            Dim monthKey As Integer = If(index < 9, first + index, (financialYear + 1) * 100 + index - 8)
            Dim fromLocked As TdsMonthFacts = Nothing
            Dim hasLocked As Boolean = locked.TryGetValue(monthKey, fromLocked)
            If monthKey < runMonthKey Then
                months.Add(If(hasLocked, fromLocked, New TdsMonthFacts(monthKey, 0D, 0D, 0D)))
            ElseIf monthKey = runMonthKey Then
                months.Add(If(hasLocked, Add(monthKey, fromLocked, thisPay), New TdsMonthFacts(monthKey, thisPay.BasicPlusDa, thisPay.HraReceived, thisPay.TaxableSalary)))
            Else
                months.Add(New TdsMonthFacts(monthKey, thisPay.BasicPlusDa, thisPay.HraReceived, thisPay.TaxableSalary))
            End If
        Next
        Return months
    End Function

    Private Shared Function Add(monthKey As Integer, a As TdsMonthFacts, b As TdsMonthFacts) As TdsMonthFacts
        Return New TdsMonthFacts(monthKey, a.BasicPlusDa + b.BasicPlusDa, a.HraReceived + b.HraReceived, a.TaxableSalary + b.TaxableSalary)
    End Function
End Class
