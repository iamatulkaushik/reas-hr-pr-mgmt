Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Known answers for leave accrual from the company setting: earned days per worked days, rounded, capped.</summary>
Public Class LeaveAccrualCalculatorTests
    <Fact>
    Public Sub OneForEveryTwenty_Nearest()
        Dim s As New LeaveAccrualSetting(1D, 20D, RateRounding.Nearest)
        Assert.Equal(1D, LeaveAccrualCalculator.Accrue(26D, 0D, s))
        Assert.Equal(2D, LeaveAccrualCalculator.Accrue(30D, 0D, s))
        Assert.Equal(0D, LeaveAccrualCalculator.Accrue(9D, 0D, s))
        Assert.Equal(1D, LeaveAccrualCalculator.Accrue(10D, 0D, s))
    End Sub

    <Fact>
    Public Sub RoundingFollowsTheSetting()
        Assert.Equal(1D, LeaveAccrualCalculator.Accrue(25D, 0D, New LeaveAccrualSetting(1D, 20D, RateRounding.Down)))
        Assert.Equal(2D, LeaveAccrualCalculator.Accrue(25D, 0D, New LeaveAccrualSetting(1D, 20D, RateRounding.Up)))
    End Sub

    <Fact>
    Public Sub Cap_StopsTheBalancePassingTheLimit()
        Dim s As New LeaveAccrualSetting(1D, 10D, RateRounding.Nearest, 30D)
        Assert.Equal(1D, LeaveAccrualCalculator.Accrue(26D, 29D, s))
        Assert.Equal(0D, LeaveAccrualCalculator.Accrue(26D, 30D, s))
        Assert.Equal(0D, LeaveAccrualCalculator.Accrue(26D, 35D, s))
    End Sub

    <Fact>
    Public Sub NoDaysWorked_NothingEarned()
        Assert.Equal(0D, LeaveAccrualCalculator.Accrue(0D, 5D, New LeaveAccrualSetting(1D, 20D, RateRounding.Nearest)))
    End Sub

    <Fact>
    Public Sub BadSettings_Throw()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New LeaveAccrualSetting(1D, 0D, RateRounding.Down))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New LeaveAccrualSetting(-1D, 20D, RateRounding.Down))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() LeaveAccrualCalculator.Accrue(-1D, 0D, New LeaveAccrualSetting(1D, 20D, RateRounding.Down)))
    End Sub
End Class
