Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Known answers worked by hand from the fixture rows (see TdsTestRules). The CA signs the expected values.</summary>
Public Class TdsProjectionServiceTests
    Private ReadOnly _rates As New FakeTdsRates()
    Private ReadOnly _service As New TdsProjectionService(_rates)

    Private Shared Function Year(monthly As Decimal, Optional basicPlusDa As Decimal = 0D, Optional hra As Decimal = 0D) As IReadOnlyList(Of TdsMonthFacts)
        Dim list As New List(Of TdsMonthFacts)()
        For index As Integer = 0 To 11
            Dim key As Integer = If(index < 9, 202604 + index, 202701 + index - 9)
            list.Add(New TdsMonthFacts(key, basicPlusDa, hra, monthly))
        Next
        Return list
    End Function

    Private Shared Function Input(months As IReadOnlyList(Of TdsMonthFacts), runKey As Integer, deducted As Decimal, bundle As TdsDeclarationBundle) As TdsProjectionInput
        Return New TdsProjectionInput(101, 2026, New Date(1990, 5, 5), runKey, months, deducted, bundle)
    End Function

    Private Shared Function Bundle(regime As TdsRegime, items As IReadOnlyList(Of TdsItem), rents As IReadOnlyList(Of TdsRentView), prev As TdsPrevEmployer) As TdsDeclarationBundle
        Return New TdsDeclarationBundle(New TdsDeclarationHeader(5, 101, 2026, regime, TdsDeclarationStatus.Submitted), items, rents, prev)
    End Function

    Private Shared Function FullYearRent() As IReadOnlyList(Of TdsRentView)
        Return New List(Of TdsRentView) From {New TdsRentView(1, 202604, 202703, 30000D, "Delhi", "A Kumar", "Friend", Nothing, "234F")}
    End Function

    <Fact>
    Public Sub NoDeclaration_NewRegimeByDefault_FirstMonth()
        Dim r = _service.Project(Input(Year(150000D), 202604, 0D, Nothing))
        Assert.True(r.Succeeded)
        Assert.True(r.Value.UsedDefaultRegime)
        Assert.Equal(TdsRegime.New, r.Value.Regime)
        Assert.Equal(1800000D, r.Value.ProjectedIncome)
        Assert.Equal(150800D, r.Value.Annual.TotalTax)
        Assert.Equal(12, r.Value.RemainingMonths)
        Assert.Equal(12567D, r.Value.TdsThisMonth)
    End Sub

    <Fact>
    Public Sub MidYear_UsesRemainingMonthsAndTaxAlreadyDeducted()
        Dim r = _service.Project(Input(Year(150000D), 202610, 75402D, Nothing))
        Assert.Equal(6, r.Value.RemainingMonths)
        Assert.Equal(12566D, r.Value.TdsThisMonth)
    End Sub

    <Fact>
    Public Sub LastMonth_TakesTheBalance()
        Dim r = _service.Project(Input(Year(150000D), 202703, 150000D, Nothing))
        Assert.Equal(1, r.Value.RemainingMonths)
        Assert.Equal(800D, r.Value.TdsThisMonth)
    End Sub

    <Fact>
    Public Sub PreviousEmployer_AddsIncome_AndCountsItsTax()
        Dim prev As New TdsPrevEmployer("ABC Ltd", Nothing, 300000D, 0D, 0D, 0D, 20000D)
        Dim r = _service.Project(Input(Year(150000D), 202604, 0D, Bundle(TdsRegime.New, New List(Of TdsItem)(), New List(Of TdsRentView)(), prev)))
        Assert.Equal(2100000D, r.Value.ProjectedIncome)
        Assert.Equal(214500D, r.Value.Annual.TotalTax)
        Assert.Equal(20000D, r.Value.TaxDeductedBefore)
        Assert.Equal(16208D, r.Value.TdsThisMonth)
    End Sub

    <Fact>
    Public Sub OldRegime_HraAndCappedDeduction()
        Dim items As New List(Of TdsItem) From {New TdsItem("80C", "", "PF", 100000D, Nothing, Nothing), New TdsItem("80C", "", "LIC", 80000D, Nothing, Nothing)}
        Dim r = _service.Project(Input(Year(120000D, 60000D, 24000D), 202604, 0D, Bundle(TdsRegime.Old, items, FullYearRent(), Nothing)))
        Assert.Equal(288000D, r.Value.HraExempt)
        Assert.Equal(150000D, r.Value.Annual.Deductions)
        Assert.Equal(952000D, r.Value.Annual.TaxableIncome)
        Assert.Equal(107016D, r.Value.Annual.TotalTax)
        Assert.Equal(8918D, r.Value.TdsThisMonth)
    End Sub

    <Fact>
    Public Sub OldRegime_VerifiedAmountReplacesDeclared()
        Dim items As New List(Of TdsItem) From {New TdsItem("80C", "", "PF", 150000D, "R1", 100000D)}
        Dim r = _service.Project(Input(Year(120000D, 60000D, 24000D), 202604, 0D, Bundle(TdsRegime.Old, items, FullYearRent(), Nothing)))
        Assert.Equal(117624D, r.Value.Annual.TotalTax)
    End Sub

    <Fact>
    Public Sub NewRegime_IgnoresDeductionsAndHra()
        Dim items As New List(Of TdsItem) From {New TdsItem("80C", "", "PF", 150000D, Nothing, Nothing)}
        Dim r = _service.Project(Input(Year(150000D, 60000D, 24000D), 202604, 0D, Bundle(TdsRegime.New, items, FullYearRent(), Nothing)))
        Assert.Equal(0D, r.Value.HraExempt)
        Assert.Equal(0D, r.Value.Annual.Deductions)
        Assert.Equal(150800D, r.Value.Annual.TotalTax)
    End Sub

    <Fact>
    Public Sub RuleFailure_StopsWithItsMessage()
        _rates.RuleFailure = "TDS rule for 2026 is not signed off."
        Dim r = _service.Project(Input(Year(150000D), 202604, 0D, Nothing))
        Assert.False(r.Succeeded)
        Assert.Equal("TDS rule for 2026 is not signed off.", r.Message)
    End Sub

    <Fact>
    Public Sub LimitFailure_StopsOldRegime()
        _rates.LimitFailure = "Limits not signed off."
        Dim r = _service.Project(Input(Year(150000D), 202604, 0D, Bundle(TdsRegime.Old, New List(Of TdsItem)(), New List(Of TdsRentView)(), Nothing)))
        Assert.False(r.Succeeded)
    End Sub

    <Fact>
    Public Sub CityListFailure_StopsOldRegimeWithRent()
        _rates.CityFailure = "City list not signed off."
        Dim r = _service.Project(Input(Year(150000D, 60000D, 24000D), 202604, 0D, Bundle(TdsRegime.Old, New List(Of TdsItem)(), FullYearRent(), Nothing)))
        Assert.False(r.Succeeded)
        Assert.Equal("City list not signed off.", r.Message)
    End Sub

    <Fact>
    Public Sub AgeBand_IsWorkedOnMarch31()
        _service.Project(New TdsProjectionInput(101, 2026, New Date(1967, 3, 31), 202604, Year(150000D), 0D, Nothing))
        Assert.Equal(TdsAgeBand.Senior, _rates.LastAgeBand)
        Assert.Equal(New Date(2026, 4, 1), _rates.LastAsOf)
    End Sub

    <Fact>
    Public Sub ElevenMonths_Fails()
        Dim months As New List(Of TdsMonthFacts)(Year(150000D))
        months.RemoveAt(11)
        Assert.False(_service.Project(Input(months, 202604, 0D, Nothing)).Succeeded)
    End Sub

    <Fact>
    Public Sub MonthsOutOfOrder_Fails()
        Dim months As New List(Of TdsMonthFacts)(Year(150000D))
        months.Reverse()
        Assert.False(_service.Project(Input(months, 202604, 0D, Nothing)).Succeeded)
    End Sub

    <Fact>
    Public Sub RunMonthOutsideYear_Fails()
        Assert.False(_service.Project(Input(Year(150000D), 202703 + 100, 0D, Nothing)).Succeeded)
    End Sub

    <Fact>
    Public Sub RemainingMonths_AprilToMarch()
        Assert.Equal(12, TdsProjectionService.RemainingMonths(202604))
        Assert.Equal(9, TdsProjectionService.RemainingMonths(202607))
        Assert.Equal(3, TdsProjectionService.RemainingMonths(202701))
        Assert.Equal(1, TdsProjectionService.RemainingMonths(202703))
    End Sub
End Class
