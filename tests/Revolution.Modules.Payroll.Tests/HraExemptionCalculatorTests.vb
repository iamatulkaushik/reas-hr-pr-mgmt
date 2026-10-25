Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Known answers worked by hand. The CA signs the expected values.</summary>
Public Class HraExemptionCalculatorTests
    Private Shared ReadOnly April As New Date(2026, 4, 1)

    Private Shared Function One(basic As Decimal, hra As Decimal, rent As Decimal, city As String) As HraMonthResult
        Return HraExemptionCalculator.Monthly(New HraMonthInput(April, basic, hra, rent, city), HraTestCities.From2026(), 10D)
    End Function

    <Fact>
    Public Sub Metro_FiftyPercentLimitIsLeast()
        Dim r As HraMonthResult = One(40000D, 20000D, 25000D, "Delhi")
        Assert.Equal(21000D, r.RentOverShare)
        Assert.Equal(20000D, r.CityLimit)
        Assert.Equal(20000D, r.Exempt)
    End Sub

    <Fact>
    Public Sub OtherCity_FortyPercentLimitIsLeast()
        Assert.Equal(16000D, One(40000D, 20000D, 25000D, "Mysuru").Exempt)
    End Sub

    <Fact>
    Public Sub Bengaluru_IsMetroFrom2026()
        Assert.Equal(20000D, One(40000D, 20000D, 25000D, "Bengaluru").Exempt)
    End Sub

    <Fact>
    Public Sub CityMatch_IgnoresCaseAndSpaces()
        Assert.Equal(20000D, One(40000D, 20000D, 25000D, "  pune ").Exempt)
    End Sub

    <Fact>
    Public Sub RentOverShare_IsLeast()
        Assert.Equal(14000D, One(40000D, 20000D, 18000D, "Bengaluru").Exempt)
    End Sub

    <Fact>
    Public Sub HraReceived_IsLeast()
        Assert.Equal(5000D, One(40000D, 5000D, 25000D, "Delhi").Exempt)
    End Sub

    <Fact>
    Public Sub RentBelowShare_NoExemption()
        Assert.Equal(0D, One(40000D, 20000D, 3000D, "Delhi").Exempt)
    End Sub

    <Fact>
    Public Sub NoRent_NoExemption()
        Assert.Equal(0D, One(40000D, 20000D, 0D, "Delhi").Exempt)
    End Sub

    <Fact>
    Public Sub NoHraReceived_NoExemption()
        Assert.Equal(0D, One(40000D, 0D, 25000D, "Delhi").Exempt)
    End Sub

    <Fact>
    Public Sub Year_SumsMonths_CityCanChangeMidYear()
        Dim months As New List(Of HraMonthInput)()
        For index As Integer = 0 To 5
            months.Add(New HraMonthInput(April.AddMonths(index), 40000D, 20000D, 25000D, "Mysuru"))
        Next
        For index As Integer = 6 To 11
            months.Add(New HraMonthInput(April.AddMonths(index), 40000D, 20000D, 25000D, "Pune"))
        Next
        Dim r As HraExemptionResult = HraExemptionCalculator.Annual(months, HraTestCities.From2026(), 10D)
        Assert.Equal(12, r.Months.Count)
        Assert.Equal(6 * 16000D + 6 * 20000D, r.TotalExempt)
    End Sub

    <Fact>
    Public Sub Year_RoundedOnceAtTheEnd()
        Dim months As New List(Of HraMonthInput) From {
            New HraMonthInput(April, 1001D, 1000D, 5000D, "Delhi"), New HraMonthInput(April.AddMonths(1), 1001D, 1000D, 5000D, "Delhi")}
        Dim r As HraExemptionResult = HraExemptionCalculator.Annual(months, HraTestCities.From2026(), 10D)
        Assert.Equal(1001D, r.TotalExempt)
    End Sub

    <Fact>
    Public Sub NoMonths_ZeroExemption()
        Assert.Equal(0D, HraExemptionCalculator.Annual(New List(Of HraMonthInput)(), HraTestCities.From2026(), 10D).TotalExempt)
    End Sub

    <Fact>
    Public Sub NegativeRent_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New HraMonthInput(April, 40000D, 20000D, -1D, "Delhi"))
    End Sub

    <Fact>
    Public Sub ShareOutOfRange_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Sub() HraExemptionCalculator.Annual(New List(Of HraMonthInput)(), HraTestCities.From2026(), 101D))
    End Sub
End Class
