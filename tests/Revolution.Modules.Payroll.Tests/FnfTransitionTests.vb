Imports Revolution.Modules.Payroll
Imports Xunit

Public Class FnfTransitionTests
    <Fact>
    Public Sub AllowedMoves()
        Assert.True(FnfTransition.CanMove(FnfStatus.Calculated, FnfStatus.Approved))
        Assert.True(FnfTransition.CanMove(FnfStatus.Approved, FnfStatus.Paid))
        Assert.True(FnfTransition.CanMove(FnfStatus.Approved, FnfStatus.Calculated))
    End Sub

    <Fact>
    Public Sub RefusedMoves()
        Assert.False(FnfTransition.CanMove(FnfStatus.Open, FnfStatus.Approved))
        Assert.False(FnfTransition.CanMove(FnfStatus.Calculated, FnfStatus.Paid))
        Assert.False(FnfTransition.CanMove(FnfStatus.Paid, FnfStatus.Calculated))
        Assert.False(FnfTransition.CanMove(FnfStatus.Paid, FnfStatus.Approved))
    End Sub

    <Fact>
    Public Sub EditOnlyBeforeApproval()
        Assert.True(FnfTransition.CanEdit(FnfStatus.Open))
        Assert.True(FnfTransition.CanEdit(FnfStatus.Calculated))
        Assert.False(FnfTransition.CanEdit(FnfStatus.Approved))
        Assert.False(FnfTransition.CanEdit(FnfStatus.Paid))
    End Sub

    <Fact>
    Public Sub MakerCannotApprove()
        Assert.False(FnfTransition.CanApprove(7, 7))
        Assert.True(FnfTransition.CanApprove(7, 8))
        Assert.True(FnfTransition.CanApprove(Nothing, 8))
    End Sub
End Class
