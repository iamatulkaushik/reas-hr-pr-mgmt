Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class SalaryHraTests
    <Theory>
    <InlineData(False, False, True, True)>
    <InlineData(True, False, True, False)>
    <InlineData(False, True, True, False)>
    <InlineData(True, False, False, True)>
    <InlineData(False, False, False, True)>
    Public Sub HraHead_CannotBeBasicOrDa(isBasic As Boolean, isDa As Boolean, isHra As Boolean, expected As Boolean)
        Dim c As New SalaryComponentItem(Nothing, "HRA", "House rent allowance", isBasic, isDa, False, 30, True, isHra)
        Assert.Equal(expected, SalaryValidator.ValidateComponent(c).Succeeded)
    End Sub

    <Fact>
    Public Sub OldCallersWithoutTheFlag_AreNotHra()
        Assert.False(New SalaryComponentItem(Nothing, "CONV", "Conveyance", False, False, False, 10, True).IsHra)
        Assert.False(New SalaryLineView(1, "BASIC", "Basic", True, False, True, 10000D).IsHra)
    End Sub
End Class
