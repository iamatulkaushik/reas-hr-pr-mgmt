Imports Revolution.Modules.Employees
Imports Xunit

Public Class EmployeeErrorMapTests
    <Fact>
    Public Sub NoCompanyInSession_IsAFault_NotARule()
        Assert.Null(EmployeeErrorMap.Translate(50002, "No company in session."))
    End Sub

    <Fact>
    Public Sub UnknownError_IsAFault()
        Assert.Null(EmployeeErrorMap.Translate(1205, "deadlock"))
    End Sub

    <Theory>
    <InlineData(51002, "Employee not found.")>
    <InlineData(51006, "The employee was changed by someone else. Reload and try again.")>
    <InlineData(51007, "Exit status must be Left, Retired, Deceased or Absconded.")>
    Public Sub ProcedureRules_AreMapped(number As Integer, expected As String)
        Assert.Equal(expected, EmployeeErrorMap.Translate(number, "ignored"))
    End Sub

    <Theory>
    <InlineData(2601, "Cannot insert duplicate key row ... unique index 'UX_EmpStat_Pan' ...", "This PAN belongs to another employee.")>
    <InlineData(2627, "Violation of UNIQUE KEY constraint 'UQ_Employee_Code'.", "This employee code already exists.")>
    <InlineData(2601, "unique index 'UX_Employee_Biometric'", "This biometric id belongs to another employee.")>
    <InlineData(547, "conflicted with the CHECK constraint ""CK_Employee_MinAge""", "Employee must be at least 14 years old on joining.")>
    <InlineData(547, "conflicted with the FOREIGN KEY constraint ""FK_Employee_Department""", "The department does not belong to the selected factory.")>
    Public Sub Constraints_AreNamedInPlainWords(number As Integer, text As String, expected As String)
        Assert.Equal(expected, EmployeeErrorMap.Translate(number, text))
    End Sub

    <Theory>
    <InlineData("CK_EmpBank_BankMode", "Bank transfer needs an account number and an IFSC.")>
    <InlineData("FK_EmployeeBank_Bank", "Bank not found.")>
    <InlineData("CK_EmpStat_Regime", "Tax regime must be old or new.")>
    Public Sub BankAndStatutoryConstraints_AreNamedInPlainWords(name As String, expected As String)
        Assert.Equal(expected, EmployeeErrorMap.Translate(547, "conflicted with the constraint """ & name & """"))
    End Sub

    <Fact>
    Public Sub UnknownConstraint_GetsAGenericMessage()
        Assert.Equal("This value already exists.", EmployeeErrorMap.Translate(2601, "some other index"))
        Assert.Equal("One of the selected values is not valid for this company.", EmployeeErrorMap.Translate(547, "some other key"))
    End Sub
End Class
