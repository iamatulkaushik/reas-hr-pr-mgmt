Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class PtCalculatorTests
    Private Shared Function Monthly() As PtRule
        Return StatutoryTestRates.Pt(PtFrequency.Monthly, 2)
    End Function

    <Theory>
    <InlineData(0, 10000, 6, 0)>
    <InlineData(0, 10001, 6, 150)>
    <InlineData(0, 15000, 6, 150)>
    <InlineData(0, 15001, 6, 200)>
    <InlineData(0, 90000, 6, 200)>
    <InlineData(0, 90000, 2, 300)>
    <InlineData(2, 25000, 6, 0)>
    <InlineData(2, 25001, 6, 200)>
    <InlineData(1, 12000, 6, 150)>
    Public Sub Slab_KnownAnswers(gender As Integer, gross As Integer, month As Integer, expected As Integer)
        Dim r As OperationResult(Of Decimal) = PtCalculator.Calculate(Monthly(), gender, CDec(gross), month)
        Assert.True(r.Succeeded)
        Assert.Equal(CDec(expected), r.Value)
    End Sub

    Private Shared Function HalfYearly() As PtRule
        Return StatutoryTestRates.Pt(PtFrequency.HalfYearly, Nothing, New List(Of Integer) From {9, 3})
    End Function

    Private Shared Function Yearly() As PtRule
        Return StatutoryTestRates.Pt(PtFrequency.Yearly, 3, New List(Of Integer) From {3})
    End Function

    <Theory>
    <InlineData(1)>
    <InlineData(6)>
    <InlineData(12)>
    Public Sub HalfYearly_MonthNotListed_IsZero(month As Integer)
        Dim r As OperationResult(Of Decimal) = PtCalculator.Calculate(HalfYearly(), 0, 20000D, month)
        Assert.True(r.Succeeded)
        Assert.Equal(0D, r.Value)
    End Sub

    <Theory>
    <InlineData(9)>
    <InlineData(3)>
    Public Sub HalfYearly_DueMonth_IsSlabAmount(month As Integer)
        Dim r As OperationResult(Of Decimal) = PtCalculator.Calculate(HalfYearly(), 0, 20000D, month)
        Assert.True(r.Succeeded)
        Assert.Equal(200D, r.Value)
    End Sub

    <Fact>
    Public Sub HalfYearly_DueMonth_SlabGap_Fails()
        Assert.False(PtCalculator.Calculate(HalfYearly(), 0, 10000.5D, 9).Succeeded)
    End Sub

    <Fact>
    Public Sub HalfYearly_MonthNotListed_NeedsNoSlab()
        Assert.Equal(0D, PtCalculator.Calculate(HalfYearly(), 0, 10000.5D, 6).Value)
    End Sub

    <Fact>
    Public Sub Yearly_DueMonthIsSpecialMonth_UsesSpecialAmount()
        Dim r As OperationResult(Of Decimal) = PtCalculator.Calculate(Yearly(), 0, 20000D, 3)
        Assert.True(r.Succeeded)
        Assert.Equal(300D, r.Value)
    End Sub

    <Theory>
    <InlineData(1)>
    <InlineData(4)>
    <InlineData(12)>
    Public Sub Yearly_OtherMonths_AreZero(month As Integer)
        Assert.Equal(0D, PtCalculator.Calculate(Yearly(), 0, 20000D, month).Value)
    End Sub

    <Fact>
    Public Sub Yearly_NoDueMonths_NeverDue()
        Dim rule As PtRule = StatutoryTestRates.Pt(PtFrequency.Yearly, Nothing)
        Assert.Equal(0D, PtCalculator.Calculate(rule, 0, 20000D, 3).Value)
    End Sub

    <Fact>
    Public Sub NoRule_SlabGap_Fails()
        Dim r As OperationResult(Of Decimal) = PtCalculator.Calculate(Monthly(), 0, 10000.5D, 6)
        Assert.False(r.Succeeded)
    End Sub

    <Fact>
    Public Sub BadMonth_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() PtCalculator.Calculate(Monthly(), 0, 1D, 13))
    End Sub

    <Fact>
    Public Sub NullRule_Throws()
        Assert.Throws(Of ArgumentNullException)(Function() PtCalculator.Calculate(Nothing, 0, 1D, 1))
    End Sub
End Class
