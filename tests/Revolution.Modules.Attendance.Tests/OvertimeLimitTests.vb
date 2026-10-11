Imports Revolution.Modules.Attendance
Imports Xunit

Public Class OvertimeLimitTests
    Private Shared ReadOnly Limits As New OvertimeLimits(600, 3000)

    <Fact>
    Public Sub UnderLimits_NoWarnings()
        Assert.Empty(OvertimeLimitChecker.Check(600, 3000, Limits))
    End Sub

    <Fact>
    Public Sub WeeklyOver_Warns()
        Assert.Contains("Weekly overtime is above the limit.", OvertimeLimitChecker.Check(601, 100, Limits))
    End Sub

    <Fact>
    Public Sub QuarterlyOver_Warns()
        Assert.Contains("Quarterly overtime is above the limit.", OvertimeLimitChecker.Check(0, 3001, Limits))
    End Sub

    <Fact>
    Public Sub ZeroLimit_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New OvertimeLimits(0, 100))
    End Sub

    <Fact>
    Public Sub NullLimits_Throws()
        Assert.Throws(Of ArgumentNullException)(Function() OvertimeLimitChecker.Check(1, 1, Nothing))
    End Sub
End Class
