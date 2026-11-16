Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Known answers: 0.75 employee and 3.25 employer on a 21,000 ceiling, rounded up. The CA signs the expected values.</summary>
Public Class EsiCalculatorTests
    <Fact>
    Public Sub AtCeiling_RoundedUp()
        Dim r As EsiResult = EsiCalculator.Calculate(21000D, 700D, StatutoryTestRates.Esi(), False)
        Assert.True(r.Eligible)
        Assert.Equal(158D, r.EmployeeShare)
        Assert.Equal(683D, r.EmployerShare)
    End Sub

    <Fact>
    Public Sub WholeRupeeAmounts_NoExtraRupee()
        Dim r As EsiResult = EsiCalculator.Calculate(18000D, 600D, StatutoryTestRates.Esi(), False)
        Assert.Equal(135D, r.EmployeeShare)
        Assert.Equal(585D, r.EmployerShare)
    End Sub

    <Fact>
    Public Sub RoundingFromRow_Nearest()
        Dim r As EsiResult = EsiCalculator.Calculate(21000D, 700D, StatutoryTestRates.Esi(RateRounding.Nearest), False)
        Assert.Equal(158D, r.EmployeeShare)
        Assert.Equal(683D, r.EmployerShare)
    End Sub

    <Fact>
    Public Sub RoundingFromRow_Down()
        Dim r As EsiResult = EsiCalculator.Calculate(21000D, 700D, StatutoryTestRates.Esi(RateRounding.Down), False)
        Assert.Equal(157D, r.EmployeeShare)
        Assert.Equal(682D, r.EmployerShare)
    End Sub

    <Fact>
    Public Sub AboveCeiling_NotEligible()
        Dim r As EsiResult = EsiCalculator.Calculate(21001D, 700D, StatutoryTestRates.Esi(), False)
        Assert.False(r.Eligible)
        Assert.Equal(0D, r.Total)
    End Sub

    <Fact>
    Public Sub AboveCeiling_StaysInForPeriod()
        Dim r As EsiResult = EsiCalculator.Calculate(22000D, 733D, StatutoryTestRates.Esi(), True)
        Assert.True(r.Eligible)
        Assert.Equal(165D, r.EmployeeShare)
        Assert.Equal(715D, r.EmployerShare)
    End Sub

    <Fact>
    Public Sub DailyWageExemption_EmployeeShareZero()
        Dim r As EsiResult = EsiCalculator.Calculate(5280D, 176D, StatutoryTestRates.Esi(RateRounding.Up, 176D), False)
        Assert.Equal(0D, r.EmployeeShare)
        Assert.Equal(172D, r.EmployerShare)
    End Sub

    <Fact>
    Public Sub ZeroWage_NotEligible()
        Assert.False(EsiCalculator.Calculate(0D, 0D, StatutoryTestRates.Esi(), True).Eligible)
    End Sub

    ''' <summary>CR 0030: the ESI ceiling for a person with a disability is 25,000 (ESIC coverage page). Known answer C06 in known_answer_cases_2026-27.xlsx.</summary>
    <Fact>
    Public Sub HigherCeiling_Flagged_24000_IsEligible()
        Dim r As EsiResult = EsiCalculator.Calculate(24000D, 800D, StatutoryTestRates.Esi(RateRounding.Up, 0D, 25000D), False, True)
        Assert.True(r.Eligible)
        Assert.Equal(180D, r.EmployeeShare)
        Assert.Equal(780D, r.EmployerShare)
    End Sub

    <Fact>
    Public Sub HigherCeiling_Flagged_AtAndAboveLimit()
        Dim rate As EsiRate = StatutoryTestRates.Esi(RateRounding.Up, 0D, 25000D)
        Assert.True(EsiCalculator.Calculate(25000D, 800D, rate, False, True).Eligible)
        Assert.False(EsiCalculator.Calculate(25001D, 800D, rate, False, True).Eligible)
    End Sub

    <Fact>
    Public Sub HigherCeiling_NotFlagged_24000_IsNotEligible()
        Assert.False(EsiCalculator.Calculate(24000D, 800D, StatutoryTestRates.Esi(RateRounding.Up, 0D, 25000D), False, False).Eligible)
    End Sub

    <Fact>
    Public Sub HigherCeiling_FlaggedAndStayEligible_AboveLimit_StaysIn()
        Assert.True(EsiCalculator.Calculate(26000D, 800D, StatutoryTestRates.Esi(RateRounding.Up, 0D, 25000D), True, True).Eligible)
    End Sub

    <Fact>
    Public Sub HigherCeiling_RowWithoutDisabledCeiling_UsesNormalCeiling()
        Dim rate As EsiRate = StatutoryTestRates.Esi()
        Assert.True(EsiCalculator.Calculate(21000D, 700D, rate, False, True).Eligible)
        Assert.False(EsiCalculator.Calculate(21001D, 700D, rate, False, True).Eligible)
    End Sub

    <Fact>
    Public Sub NullRate_Throws()
        Assert.Throws(Of ArgumentNullException)(Function() EsiCalculator.Calculate(1D, 1D, Nothing, False))
    End Sub
End Class
