Imports Revolution.Core

''' <summary>
''' Income tax on a bonus (Owner 2026-11-04: the bonus run carries income tax). The twelve months of the pay month's financial year are filled like the regular run does: a month already in locked pay
''' takes its figures from there, an earlier month with none is zero, the pay month and the months after it with no locked pay repeat the latest locked month up to the pay month (zero when there is none).
''' The tax on the bonus is the year's tax with the bonus added to the pay month less the year's tax without it, never below zero. Pure: the projection is passed in. It takes the whole of that tax in the bonus run.
''' </summary>
Public NotInheritable Class BonusTaxPlanner
    Private Sub New()
    End Sub

    Public Shared Function Plan(period As RunPeriod, bonus As Decimal, tax As EmployeeTaxFacts, project As Func(Of TdsProjectionInput, OperationResult(Of TdsProjectionResult))) As OperationResult(Of Decimal)
        If period Is Nothing Then Throw New ArgumentNullException(NameOf(period))
        If tax Is Nothing Then Throw New ArgumentNullException(NameOf(tax))
        If project Is Nothing Then Throw New ArgumentNullException(NameOf(project))
        If bonus <= 0D Then Return OperationResult(Of Decimal).Ok(0D)
        Dim financialYear As Integer = FinancialYearCalendar.StartYear(period.PeriodTo)
        Dim without As OperationResult(Of TdsProjectionResult) = project(Input(financialYear, period.MonthKey, 0D, tax))
        If Not without.Succeeded Then Return OperationResult(Of Decimal).Fail(without.Message)
        Dim [with] As OperationResult(Of TdsProjectionResult) = project(Input(financialYear, period.MonthKey, bonus, tax))
        If Not [with].Succeeded Then Return OperationResult(Of Decimal).Fail([with].Message)
        Dim extra As Decimal = [with].Value.Annual.TotalTax - without.Value.Annual.TotalTax
        Return OperationResult(Of Decimal).Ok(Rupee.Round(Math.Max(0D, extra), RateRounding.Nearest))
    End Function

    Private Shared Function Input(financialYear As Integer, runMonthKey As Integer, bonus As Decimal, tax As EmployeeTaxFacts) As TdsProjectionInput
        Dim locked As New Dictionary(Of Integer, TdsMonthFacts)()
        For Each month As TdsMonthFacts In tax.LockedMonths
            locked(month.MonthKey) = month
        Next
        Dim repeat As TdsMonthFacts = Nothing
        For Each month As TdsMonthFacts In tax.LockedMonths.OrderBy(Function(m As TdsMonthFacts) m.MonthKey)
            If month.MonthKey <= runMonthKey Then repeat = month
        Next
        Dim first As Integer = financialYear * 100 + 4
        Dim months As New List(Of TdsMonthFacts)(12)
        For index As Integer = 0 To 11
            Dim monthKey As Integer = If(index < 9, first + index, (financialYear + 1) * 100 + index - 8)
            Dim fromLocked As TdsMonthFacts = Nothing
            Dim hasLocked As Boolean = locked.TryGetValue(monthKey, fromLocked)
            Dim basis As TdsMonthFacts
            If hasLocked Then
                basis = fromLocked
            ElseIf monthKey >= runMonthKey AndAlso repeat IsNot Nothing Then
                basis = New TdsMonthFacts(monthKey, repeat.BasicPlusDa, repeat.HraReceived, repeat.TaxableSalary)
            Else
                basis = New TdsMonthFacts(monthKey, 0D, 0D, 0D)
            End If
            If monthKey = runMonthKey AndAlso bonus > 0D Then basis = New TdsMonthFacts(monthKey, basis.BasicPlusDa, basis.HraReceived, basis.TaxableSalary + bonus)
            months.Add(basis)
        Next
        Return New TdsProjectionInput(tax.EmployeeId, financialYear, tax.DateOfBirth, runMonthKey, months, tax.TaxDeductedBefore, tax.Declaration)
    End Function
End Class
