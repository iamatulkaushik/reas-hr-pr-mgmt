Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>Worked by hand: gross 26,000 over a divisor of 26 is 1,000 a day.</summary>
Public Class NoticePayCalculatorTests
    <Fact>
    Public Sub TwentyDaysShort_RecoversTwentyDays()
        Dim r As NoticePayResult = NoticePayCalculator.Calculate(30, 10, 1000D)
        Assert.Equal(20, r.ShortDays)
        Assert.Equal(20000D, r.Amount)
    End Sub

    <Fact>
    Public Sub FullNoticeServed_RecoversNothing()
        Assert.Equal(0D, NoticePayCalculator.Calculate(30, 30, 1000D).Amount)
    End Sub

    <Fact>
    Public Sub MoreThanNoticeServed_RecoversNothing()
        Dim r As NoticePayResult = NoticePayCalculator.Calculate(30, 45, 1000D)
        Assert.Equal(0, r.ShortDays)
        Assert.Equal(0D, r.Amount)
    End Sub

    <Fact>
    Public Sub Amount_RoundsOnceToNearestRupee()
        Assert.Equal(1154D, NoticePayCalculator.Calculate(30, 29, 1153.5D).Amount)
    End Sub

    <Fact>
    Public Sub ServedDays_CountsBothEnds()
        Assert.Equal(10, NoticePayCalculator.ServedDays(New Date(2026, 10, 1), New Date(2026, 10, 10)))
    End Sub

    <Fact>
    Public Sub ServedDays_NoNoticeDate_IsZero()
        Assert.Equal(0, NoticePayCalculator.ServedDays(Nothing, New Date(2026, 10, 10)))
    End Sub

    <Fact>
    Public Sub ServedDays_LastDayBeforeNotice_IsZero()
        Assert.Equal(0, NoticePayCalculator.ServedDays(New Date(2026, 10, 10), New Date(2026, 10, 1)))
    End Sub

    <Fact>
    Public Sub NegativeInputs_Throw()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() NoticePayCalculator.Calculate(-1, 0, 1D))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() NoticePayCalculator.Calculate(30, -1, 1D))
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() NoticePayCalculator.Calculate(30, 0, -1D))
    End Sub
End Class
