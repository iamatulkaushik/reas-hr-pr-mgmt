Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Known answers: 12 percent employee, 3.67 and 8.33 employer, 0.5 EDLI and 0.5 admin, ceiling 15,000. The CA signs the expected values.</summary>
Public Class EpfCalculatorTests
    <Fact>
    Public Sub AtCeiling_KnownAnswer()
        Dim r As EpfResult = EpfCalculator.Calculate(15000D, StatutoryTestRates.Epf())
        Assert.Equal(1800D, r.EmployeeShare)
        Assert.Equal(1250D, r.EmployerEps)
        Assert.Equal(550D, r.EmployerEpf)
        Assert.Equal(75D, r.Edli)
        Assert.Equal(75D, r.Admin)
    End Sub

    <Fact>
    Public Sub BelowCeiling_KnownAnswer()
        Dim r As EpfResult = EpfCalculator.Calculate(10000D, StatutoryTestRates.Epf())
        Assert.Equal(1200D, r.EmployeeShare)
        Assert.Equal(833D, r.EmployerEps)
        Assert.Equal(367D, r.EmployerEpf)
        Assert.Equal(50D, r.Edli)
        Assert.Equal(50D, r.Admin)
    End Sub

    <Fact>
    Public Sub AboveCeiling_UsesCeiling()
        Dim r As EpfResult = EpfCalculator.Calculate(40000D, StatutoryTestRates.Epf())
        Assert.Equal(15000D, r.ContributoryWage)
        Assert.Equal(1800D, r.EmployeeShare)
    End Sub

    <Fact>
    Public Sub AboveCeiling_ActualWageByChoice()
        Dim r As EpfResult = EpfCalculator.Calculate(20000D, StatutoryTestRates.Epf(), True)
        Assert.Equal(2400D, r.EmployeeShare)
        Assert.Equal(1250D, r.EmployerEps)
        Assert.Equal(1150D, r.EmployerEpf)
        Assert.Equal(75D, r.Edli)
    End Sub

    <Fact>
    Public Sub EmployerShare_EqualsEmployeeShare()
        Dim r As EpfResult = EpfCalculator.Calculate(13579D, StatutoryTestRates.Epf())
        Assert.Equal(r.EmployeeShare, r.EmployerShare)
    End Sub

    <Fact>
    Public Sub ZeroWage_AllZero()
        Dim r As EpfResult = EpfCalculator.Calculate(0D, StatutoryTestRates.Epf())
        Assert.Equal(0D, r.EmployeeShare + r.EmployerShare + r.Edli + r.Admin)
    End Sub

    <Fact>
    Public Sub NegativeWage_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() EpfCalculator.Calculate(-1D, StatutoryTestRates.Epf()))
    End Sub

    <Fact>
    Public Sub NullRate_Throws()
        Assert.Throws(Of ArgumentNullException)(Function() EpfCalculator.Calculate(1D, Nothing))
    End Sub
End Class
