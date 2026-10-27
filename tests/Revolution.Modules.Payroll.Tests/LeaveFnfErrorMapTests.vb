Imports Revolution.Modules.Payroll
Imports Xunit

Public Class LeaveFnfErrorMapTests
    <Theory>
    <InlineData(50350)>
    <InlineData(50351)>
    <InlineData(50352)>
    <InlineData(50353)>
    <InlineData(50354)>
    <InlineData(50355)>
    <InlineData(50356)>
    <InlineData(50357)>
    <InlineData(50358)>
    <InlineData(50359)>
    Public Sub LeaveAndFnfRuleNumbers_KeepTheirText(number As Integer)
        Assert.Equal("Some rule text.", PayrollErrorMap.Translate(number, "Some rule text."))
    End Sub

    <Fact>
    Public Sub LeaveTypeCodeDuplicate_IsTranslated()
        Assert.Equal("A leave type with this code already exists.", PayrollErrorMap.Translate(2627, "Violation of UNIQUE KEY constraint 'UQ_LeaveType_Code'."))
    End Sub

    <Fact>
    Public Sub FnfSeparationDuplicate_IsTranslated()
        Assert.Equal("A settlement already exists for this separation.", PayrollErrorMap.Translate(2627, "Violation of UNIQUE KEY constraint 'UQ_FnfSettlement_Separation'."))
    End Sub

    <Fact>
    Public Sub LeaveLedgerForeignKeys_AreTranslated()
        Assert.Equal("Employee not found.", PayrollErrorMap.Translate(547, "conflicted with the FOREIGN KEY constraint 'FK_LeaveLedger_Employee'."))
        Assert.Equal("Leave type not found.", PayrollErrorMap.Translate(547, "conflicted with the FOREIGN KEY constraint 'FK_LeaveLedger_Type'."))
    End Sub

    <Fact>
    Public Sub SessionFaults_StayFaults()
        Assert.Null(PayrollErrorMap.Translate(50002, "No company in session."))
    End Sub
End Class
