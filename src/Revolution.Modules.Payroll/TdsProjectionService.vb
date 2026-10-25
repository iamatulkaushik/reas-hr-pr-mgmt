Imports Revolution.Core

''' <summary>
''' Works out the year's tax and this month's TDS for one employee from the rates, the declaration and the twelve months of salary. Pure apart from the rate
''' lookups: no database. Any failed rate lookup (missing, or not signed off by the CA) is returned as a failure and payroll stops TDS for that run; it never guesses.
''' Rules kept here: no declaration means the new regime; old regime takes the declared deductions and the HRA exemption, new regime neither; a deduction item counts
''' at its verified amount when proof was checked, otherwise its declared amount. Previous employer salary and perquisites are added to income and its tax counts as
''' already deducted. Its provident fund and professional tax are not added to deductions (the Associate declares them as items): pending the CA.
''' HraRentSharePct is the share of salary taken off rent in the HRA rule. It has no stat table yet (CR 0018), so it is a flagged constant until then.
''' </summary>
Public NotInheritable Class TdsProjectionService
    Friend Const HraRentSharePct As Decimal = 10D

    Private ReadOnly _rates As IStatutoryRateService

    Public Sub New(rates As IStatutoryRateService)
        If rates Is Nothing Then Throw New ArgumentNullException(NameOf(rates))
        _rates = rates
    End Sub

    Public Function Project(input As TdsProjectionInput) As OperationResult(Of TdsProjectionResult)
        Dim check As OperationResult = Validate(input)
        If Not check.Succeeded Then Return OperationResult(Of TdsProjectionResult).Fail(check.Message)
        Dim declaration As TdsDeclarationBundle = input.Declaration
        Dim hasDeclaration As Boolean = declaration IsNot Nothing
        Dim regime As TdsRegime = If(hasDeclaration, declaration.Header.Regime, TdsRegime.New)
        Dim asOf As New Date(input.RunMonthKey \ 100, input.RunMonthKey Mod 100, 1)
        Dim ruleResult As OperationResult(Of TdsRule) = _rates.GetTdsRule(asOf, regime, TdsAgeBandResolver.Resolve(input.DateOfBirth, input.FinancialYear))
        If Not ruleResult.Succeeded Then Return OperationResult(Of TdsProjectionResult).Fail(ruleResult.Message)
        Dim limits As IReadOnlyList(Of TdsDeductionLimit) = Nothing
        Dim hra As Decimal = 0D
        If regime = TdsRegime.Old AndAlso hasDeclaration Then
            Dim limitResult As OperationResult(Of IReadOnlyList(Of TdsDeductionLimit)) = _rates.GetTdsDeductionLimits(asOf, regime)
            If Not limitResult.Succeeded Then Return OperationResult(Of TdsProjectionResult).Fail(limitResult.Message)
            limits = limitResult.Value
            Dim hraResult As OperationResult(Of Decimal) = ComputeHra(input, asOf)
            If Not hraResult.Succeeded Then Return OperationResult(Of TdsProjectionResult).Fail(hraResult.Message)
            hra = hraResult.Value
        End If
        Return OperationResult(Of TdsProjectionResult).Ok(Build(input, regime, Not hasDeclaration, ruleResult.Value, limits, hra))
    End Function

    ''' <summary>Months from the run month to March, both included.</summary>
    Public Shared Function RemainingMonths(runMonthKey As Integer) As Integer
        Dim month As Integer = runMonthKey Mod 100
        Return If(month >= 4, 16 - month, 4 - month)
    End Function

    Private Shared Function Build(input As TdsProjectionInput, regime As TdsRegime, usedDefault As Boolean, rule As TdsRule,
                                  limits As IReadOnlyList(Of TdsDeductionLimit), hra As Decimal) As TdsProjectionResult
        Dim salary As Decimal = 0D
        For Each facts As TdsMonthFacts In input.Months
            salary += facts.TaxableSalary
        Next
        Dim prev As TdsPrevEmployer = If(input.Declaration Is Nothing, Nothing, input.Declaration.Prev)
        Dim income As Decimal = salary + If(prev Is Nothing, 0D, prev.SalaryIncome + prev.Perquisites)
        Dim annual As TdsAnnualResult = TdsCalculator.Annual(Math.Max(0D, income - hra), ClaimsOf(input.Declaration), rule, limits)
        Dim deducted As Decimal = input.TaxDeductedBefore + If(prev Is Nothing, 0D, prev.TaxDeducted)
        Dim remaining As Integer = RemainingMonths(input.RunMonthKey)
        Return New TdsProjectionResult(regime, usedDefault, income, hra, annual, deducted, remaining, TdsCalculator.Monthly(annual.TotalTax, deducted, remaining))
    End Function

    Private Function ComputeHra(input As TdsProjectionInput, asOf As Date) As OperationResult(Of Decimal)
        Dim rents As IReadOnlyList(Of TdsRentView) = input.Declaration.Rents
        If rents Is Nothing OrElse rents.Count = 0 Then Return OperationResult(Of Decimal).Ok(0D)
        Dim cityResult As OperationResult(Of HraMetroList) = _rates.GetHraMetroList(asOf)
        If Not cityResult.Succeeded Then Return OperationResult(Of Decimal).Fail(cityResult.Message)
        Dim months As New List(Of HraMonthInput)()
        For Each facts As TdsMonthFacts In input.Months
            Dim rent As TdsRentView = RentFor(rents, facts.MonthKey)
            If rent IsNot Nothing Then
                months.Add(New HraMonthInput(New Date(facts.MonthKey \ 100, facts.MonthKey Mod 100, 1), facts.BasicPlusDa, facts.HraReceived, rent.MonthlyRent, rent.City))
            End If
        Next
        Return OperationResult(Of Decimal).Ok(HraExemptionCalculator.Annual(months, cityResult.Value, HraRentSharePct).TotalExempt)
    End Function

    Private Shared Function RentFor(rents As IReadOnlyList(Of TdsRentView), monthKey As Integer) As TdsRentView
        For Each rent As TdsRentView In rents
            If monthKey >= rent.FromMonthKey AndAlso monthKey <= rent.ToMonthKey Then Return rent
        Next
        Return Nothing
    End Function

    Private Shared Function ClaimsOf(declaration As TdsDeclarationBundle) As IEnumerable(Of TdsDeductionClaim)
        Dim claims As New List(Of TdsDeductionClaim)()
        If declaration Is Nothing OrElse declaration.Items Is Nothing Then Return claims
        For Each item As TdsItem In declaration.Items
            claims.Add(New TdsDeductionClaim(item.SectionCode, item.SubLimitKey, item.EffectiveAmount))
        Next
        Return claims
    End Function

    Private Shared Function Validate(input As TdsProjectionInput) As OperationResult
        If input Is Nothing Then Return OperationResult.Fail("Projection input is missing.")
        If input.Months Is Nothing OrElse input.Months.Count <> 12 Then Return OperationResult.Fail("Twelve months of salary are needed, April to March.")
        Dim first As Integer = input.FinancialYear * 100 + 4
        For index As Integer = 0 To 11
            Dim expected As Integer = If(index < 9, first + index, (input.FinancialYear + 1) * 100 + index - 8)
            If input.Months(index).MonthKey <> expected Then Return OperationResult.Fail("Months must run April to March in order.")
            If input.Months(index).TaxableSalary < 0D OrElse input.Months(index).BasicPlusDa < 0D OrElse input.Months(index).HraReceived < 0D Then
                Return OperationResult.Fail("Salary amounts cannot be negative.")
            End If
        Next
        If input.RunMonthKey < first OrElse input.RunMonthKey > (input.FinancialYear + 1) * 100 + 3 Then Return OperationResult.Fail("The run month is outside the financial year.")
        If input.TaxDeductedBefore < 0D Then Return OperationResult.Fail("Tax already deducted cannot be negative.")
        Return OperationResult.Ok()
    End Function
End Class
