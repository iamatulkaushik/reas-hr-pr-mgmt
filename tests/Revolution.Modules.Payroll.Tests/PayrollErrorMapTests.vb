Imports Revolution.Modules.Payroll
Imports Xunit

Public Class PayrollErrorMapTests
    <Theory>
    <InlineData(50301)>
    <InlineData(50302)>
    <InlineData(50303)>
    <InlineData(50304)>
    <InlineData(50311)>
    <InlineData(50312)>
    <InlineData(50314)>
    <InlineData(50317)>
    <InlineData(50313)>
    <InlineData(50315)>
    <InlineData(50316)>
    <InlineData(50318)>
    <InlineData(50319)>
    <InlineData(50321)>
    <InlineData(50324)>
    <InlineData(50328)>
    <InlineData(50329)>
    <InlineData(50340)>
    <InlineData(50343)>
    <InlineData(50345)>
    Public Sub RuleNumbers_KeepProcedureText(number As Integer)
        Assert.Equal("Text from the procedure.", PayrollErrorMap.Translate(number, "Text from the procedure."))
    End Sub

    <Theory>
    <InlineData(50002)>
    <InlineData(50330)>
    <InlineData(50001)>
    <InlineData(1205)>
    <InlineData(547)>
    <InlineData(2627)>
    Public Sub FaultNumbers_AreNotMapped(number As Integer)
        Assert.Null(PayrollErrorMap.Translate(number, "Something else."))
    End Sub

    <Fact>
    Public Sub DuplicateComponentCode_IsMapped()
        Dim text As String = "Violation of UNIQUE KEY constraint 'UQ_SalaryComponent_Code'. Cannot insert duplicate key."
        Assert.Equal("A component with this code already exists.", PayrollErrorMap.Translate(2627, text))
        Assert.Equal("A component with this code already exists.", PayrollErrorMap.Translate(2601, text))
    End Sub

    <Fact>
    Public Sub BasicAndDaCheck_IsMapped()
        Assert.Equal("A component cannot be both basic and DA.", PayrollErrorMap.Translate(547, "conflicted with the CHECK constraint ""CK_SalaryComponent_Kind"""))
    End Sub

    <Fact>
    Public Sub UnknownEmployee_IsMapped()
        Assert.Equal("Employee not found.", PayrollErrorMap.Translate(547, "conflicted with the FOREIGN KEY constraint ""FK_EmployeeSalary_Employee"""))
    End Sub

    <Fact>
    Public Sub LoanConstraints_AreMapped()
        Assert.Equal("Employee not found.", PayrollErrorMap.Translate(547, "conflicted with the FOREIGN KEY constraint ""FK_Loan_Employee"""))
        Assert.Equal("Loan type not found.", PayrollErrorMap.Translate(547, "conflicted with the FOREIGN KEY constraint ""FK_Loan_Type"""))
        Assert.Equal("A loan type with this name already exists.", PayrollErrorMap.Translate(2627, "Violation of UNIQUE KEY constraint 'UQ_LoanType_Name'."))
    End Sub

    <Fact>
    Public Sub RuleNumber_WithEmptyText_IsNotMapped()
        Assert.Null(PayrollErrorMap.Translate(50301, ""))
    End Sub
End Class
