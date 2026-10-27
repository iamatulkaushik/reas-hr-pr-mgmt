Imports Revolution.Modules.Payroll
Imports Xunit

Public Class FnfDueWindowTests
    <Fact>
    Public Sub Window_IsFirstToSeventhOfNextMonth()
        Dim w As RunPeriod = FnfDueWindow.For(New Date(2026, 10, 14))
        Assert.Equal(New Date(2026, 11, 1), w.PeriodFrom)
        Assert.Equal(New Date(2026, 11, 7), w.PeriodTo)
    End Sub

    <Fact>
    Public Sub DecemberExit_RollsIntoJanuary()
        Dim w As RunPeriod = FnfDueWindow.For(New Date(2026, 12, 31))
        Assert.Equal(New Date(2027, 1, 1), w.PeriodFrom)
        Assert.Equal(New Date(2027, 1, 7), w.PeriodTo)
    End Sub

    <Fact>
    Public Sub IsInWindow_EdgesIncluded()
        Dim exitDate As New Date(2026, 10, 14)
        Assert.True(FnfDueWindow.IsInWindow(exitDate, New Date(2026, 11, 1)))
        Assert.True(FnfDueWindow.IsInWindow(exitDate, New Date(2026, 11, 7)))
        Assert.False(FnfDueWindow.IsInWindow(exitDate, New Date(2026, 10, 31)))
        Assert.False(FnfDueWindow.IsInWindow(exitDate, New Date(2026, 11, 8)))
    End Sub
End Class
