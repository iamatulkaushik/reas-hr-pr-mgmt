Imports Revolution.Modules.Payroll
Imports Xunit

Public Class PayCycleTests
    <Fact>
    Public Sub CalendarMonth_IsFirstToLastDay()
        Dim p As RunPeriod = PayCycle.PeriodFor(2026, 10, 1)
        Assert.Equal(New Date(2026, 10, 1), p.PeriodFrom)
        Assert.Equal(New Date(2026, 10, 31), p.PeriodTo)
        Assert.Equal(31, p.Days)
    End Sub

    <Fact>
    Public Sub CalendarMonth_February()
        Dim p As RunPeriod = PayCycle.PeriodFor(2026, 2, 1)
        Assert.Equal(28, p.Days)
    End Sub

    <Fact>
    Public Sub TwentySixth_RunsFromPreviousMonth()
        Dim p As RunPeriod = PayCycle.PeriodFor(2026, 10, 26)
        Assert.Equal(New Date(2026, 9, 26), p.PeriodFrom)
        Assert.Equal(New Date(2026, 10, 25), p.PeriodTo)
        Assert.Equal(30, p.Days)
        Assert.Equal(10, p.WageMonth)
        Assert.Equal(202610, p.MonthKey)
    End Sub

    <Fact>
    Public Sub TwentySixth_JanuaryStartsInDecember()
        Dim p As RunPeriod = PayCycle.PeriodFor(2026, 1, 26)
        Assert.Equal(New Date(2025, 12, 26), p.PeriodFrom)
        Assert.Equal(New Date(2026, 1, 25), p.PeriodTo)
        Assert.Equal(31, p.Days)
    End Sub

    <Fact>
    Public Sub TwentySixth_FebruaryHasThirtyOneDays()
        Assert.Equal(31, PayCycle.PeriodFor(2026, 2, 26).Days)
    End Sub

    <Fact>
    Public Sub OtherStartDay_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Sub() PayCycle.PeriodFor(2026, 10, 15))
    End Sub

    <Fact>
    Public Sub RateDate_IsPeriodEnd()
        Assert.Equal(New Date(2026, 10, 25), PayCycle.PeriodFor(2026, 10, 26).RateDate)
    End Sub
End Class
