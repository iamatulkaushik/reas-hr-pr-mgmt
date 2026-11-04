Imports Revolution.Modules.Payroll
Imports Xunit

Public Class ArrearsRecoveryPlannerTests
    Private Shared Function Due(claim As Integer, employee As Integer, code As String, amount As Decimal) As ArrearsRecoveryDue
        Return New ArrearsRecoveryDue(claim, employee, code, amount)
    End Function

    <Fact>
    Public Sub Plan_NoClaims_LeavesFactsAsTheyAre()
        Dim facts As New List(Of RunEmployeeFacts) From {RunTestData.Facts(101, "E101")}
        Dim plan As ArrearsRecoveryPlan = ArrearsRecoveryPlanner.Plan(facts, New List(Of ArrearsRecoveryDue)())
        Assert.Single(plan.Facts)
        Assert.Equal(0D, plan.Facts(0).OtherDeductions)
        Assert.Empty(plan.ClaimIds)
        Assert.Empty(plan.NotInRun)
    End Sub

    <Fact>
    Public Sub Plan_AddsTheClaimToOtherDeductions_OnlyForThatEmployee()
        Dim facts As New List(Of RunEmployeeFacts) From {RunTestData.Facts(101, "E101"), RunTestData.Facts(102, "E102")}
        Dim plan As ArrearsRecoveryPlan = ArrearsRecoveryPlanner.Plan(facts, New List(Of ArrearsRecoveryDue) From {Due(7, 102, "E102", 1500D)})
        Assert.Equal(0D, plan.Facts(0).OtherDeductions)
        Assert.Equal(1500D, plan.Facts(1).OtherDeductions)
        Assert.Equal(New List(Of Integer) From {7}, plan.ClaimIds)
    End Sub

    <Fact>
    Public Sub Plan_TwoClaimsOfOneEmployee_AreAddedTogether()
        Dim facts As New List(Of RunEmployeeFacts) From {RunTestData.Facts(101, "E101")}
        Dim plan As ArrearsRecoveryPlan = ArrearsRecoveryPlanner.Plan(facts, New List(Of ArrearsRecoveryDue) From {Due(9, 101, "E101", 400D), Due(3, 101, "E101", 600D)})
        Assert.Equal(1000D, plan.Facts(0).OtherDeductions)
        Assert.Equal(New List(Of Integer) From {3, 9}, plan.ClaimIds)
    End Sub

    <Fact>
    Public Sub Plan_KeepsOtherFactsUntouched()
        Dim original As RunEmployeeFacts = RunTestData.Facts(101, "E101", paidDays:=28D, carryIn:=250D)
        Dim plan As ArrearsRecoveryPlan = ArrearsRecoveryPlanner.Plan(New List(Of RunEmployeeFacts) From {original}, New List(Of ArrearsRecoveryDue) From {Due(1, 101, "E101", 100D)})
        Dim changed As RunEmployeeFacts = plan.Facts(0)
        Assert.Equal(28D, changed.PaidDays)
        Assert.Equal(250D, changed.CarryForwardIn)
        Assert.Equal(original.Salary.Count, changed.Salary.Count)
        Assert.Equal("E101", changed.EmpCode)
        Assert.Equal(original.IsEpfMember, changed.IsEpfMember)
    End Sub

    <Fact>
    Public Sub Plan_EmployeeWithoutPay_IsNamedAndTheClaimStaysOut()
        Dim facts As New List(Of RunEmployeeFacts) From {RunTestData.Facts(101, "E101")}
        Dim plan As ArrearsRecoveryPlan = ArrearsRecoveryPlanner.Plan(facts, New List(Of ArrearsRecoveryDue) From {Due(5, 205, "E205", 800D), Due(6, 101, "E101", 200D)})
        Assert.Equal(New List(Of String) From {"E205"}, plan.NotInRun)
        Assert.Equal(New List(Of Integer) From {6}, plan.ClaimIds)
        Assert.Equal(200D, plan.Facts(0).OtherDeductions)
    End Sub

    <Fact>
    Public Sub Plan_AddsToExistingOtherDeductions()
        Dim base As RunEmployeeFacts = RunTestData.Facts(101, "E101").WithOtherDeductions(300D)
        Dim plan As ArrearsRecoveryPlan = ArrearsRecoveryPlanner.Plan(New List(Of RunEmployeeFacts) From {base}, New List(Of ArrearsRecoveryDue) From {Due(1, 101, "E101", 700D)})
        Assert.Equal(1000D, plan.Facts(0).OtherDeductions)
    End Sub

    <Fact>
    Public Sub Due_AmountMustBeAboveZero()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() New ArrearsRecoveryDue(1, 101, "E101", 0D))
    End Sub
End Class
