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

    <Fact>
    Public Sub NotMonthly_Fails()
        Assert.False(PtCalculator.Calculate(StatutoryTestRates.Pt(PtFrequency.HalfYearly, Nothing), 0, 20000D, 6).Succeeded)
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
