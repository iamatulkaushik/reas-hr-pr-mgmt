Imports Revolution.Modules.Payroll
Imports Xunit

Public Class RunStateMachineTests
    <Theory>
    <InlineData(2, 1)>
    <InlineData(2, 3)>
    <InlineData(2, 4)>
    <InlineData(3, 4)>
    <InlineData(3, 1)>
    <InlineData(4, 5)>
    <InlineData(5, 6)>
    <InlineData(5, 1)>
    Public Sub AllowedChanges(fromStatus As Integer, toStatus As Integer)
        Assert.True(RunStateMachine.CanChange(CType(fromStatus, PayrollRunStatus), CType(toStatus, PayrollRunStatus)))
    End Sub

    <Theory>
    <InlineData(1, 2)>
    <InlineData(1, 3)>
    <InlineData(1, 4)>
    <InlineData(3, 5)>
    <InlineData(3, 2)>
    <InlineData(4, 1)>
    <InlineData(4, 6)>
    <InlineData(6, 1)>
    <InlineData(6, 5)>
    <InlineData(5, 4)>
    Public Sub BlockedChanges(fromStatus As Integer, toStatus As Integer)
        Assert.False(RunStateMachine.CanChange(CType(fromStatus, PayrollRunStatus), CType(toStatus, PayrollRunStatus)))
    End Sub

    <Fact>
    Public Sub OnlyReopenNeedsReason()
        Assert.True(RunStateMachine.NeedsReason(PayrollRunStatus.Locked, PayrollRunStatus.Draft))
        Assert.False(RunStateMachine.NeedsReason(PayrollRunStatus.Submitted, PayrollRunStatus.Draft))
    End Sub

    <Fact>
    Public Sub LinesEditableOnlyInDraftAndProcessed()
        Assert.True(RunStateMachine.CanEditLines(PayrollRunStatus.Draft))
        Assert.True(RunStateMachine.CanEditLines(PayrollRunStatus.Processed))
        Assert.False(RunStateMachine.CanEditLines(PayrollRunStatus.Submitted))
        Assert.False(RunStateMachine.CanEditLines(PayrollRunStatus.Approved))
        Assert.False(RunStateMachine.CanEditLines(PayrollRunStatus.Locked))
        Assert.False(RunStateMachine.CanEditLines(PayrollRunStatus.Paid))
    End Sub
End Class
