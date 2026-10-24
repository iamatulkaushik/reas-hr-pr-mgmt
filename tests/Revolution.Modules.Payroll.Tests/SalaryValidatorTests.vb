Imports Revolution.Modules.Payroll
Imports Xunit

Public Class SalaryValidatorTests
    Private Shared ReadOnly Components As IReadOnlyList(Of SalaryComponentItem) = New FakeSalaryRepository().Components

    Private Shared Function Check(ParamArray pairs() As Decimal) As Boolean
        Dim list As New List(Of SalaryLineWrite)()
        For i As Integer = 0 To pairs.Length - 1 Step 2
            list.Add(New SalaryLineWrite(CInt(pairs(i)), pairs(i + 1)))
        Next
        Return SalaryValidator.ValidateSalary(101, New Date(2026, 11, 1), Nothing, list, Components).Succeeded
    End Function

    <Fact>
    Public Sub Salary_BasicOnly_Passes()
        Assert.True(Check(1D, 15000D))
    End Sub

    <Fact>
    Public Sub Salary_WithoutBasicOrDa_Fails()
        Assert.False(Check(3D, 2000D))
    End Sub

    <Fact>
    Public Sub Salary_ZeroBasic_Fails()
        Assert.False(Check(1D, 0D, 3D, 2000D))
    End Sub

    <Fact>
    Public Sub Salary_DaAlone_Passes()
        Assert.True(Check(2D, 3000D))
    End Sub

    <Fact>
    Public Sub Salary_ComponentTwice_Fails()
        Assert.False(Check(1D, 15000D, 1D, 100D))
    End Sub

    <Fact>
    Public Sub Salary_UnknownComponent_Fails()
        Assert.False(Check(1D, 15000D, 77D, 100D))
    End Sub

    <Fact>
    Public Sub Salary_InactiveComponent_Fails()
        Assert.False(Check(1D, 15000D, 4D, 100D))
    End Sub

    <Fact>
    Public Sub Salary_NegativeAmount_Fails()
        Assert.False(Check(1D, 15000D, 3D, -1D))
    End Sub

    <Fact>
    Public Sub Salary_ThreeDecimals_Fails()
        Assert.False(Check(1D, 15000.125D))
    End Sub

    <Fact>
    Public Sub Salary_AboveColumnLimit_Fails()
        Assert.False(Check(1D, 1000000000000D))
    End Sub

    <Fact>
    Public Sub Salary_NoLines_Fails()
        Assert.False(SalaryValidator.ValidateSalary(101, New Date(2026, 11, 1), Nothing, New List(Of SalaryLineWrite)(), Components).Succeeded)
    End Sub

    <Fact>
    Public Sub Salary_NoEmployee_Fails()
        Dim lines As New List(Of SalaryLineWrite) From {New SalaryLineWrite(1, 15000D)}
        Assert.False(SalaryValidator.ValidateSalary(0, New Date(2026, 11, 1), Nothing, lines, Components).Succeeded)
    End Sub

    <Fact>
    Public Sub Salary_DateBeforeYear2000_Fails()
        Dim lines As New List(Of SalaryLineWrite) From {New SalaryLineWrite(1, 15000D)}
        Assert.False(SalaryValidator.ValidateSalary(101, New Date(1999, 12, 31), Nothing, lines, Components).Succeeded)
    End Sub

    <Fact>
    Public Sub Salary_ReasonOver200_Fails()
        Dim lines As New List(Of SalaryLineWrite) From {New SalaryLineWrite(1, 15000D)}
        Assert.False(SalaryValidator.ValidateSalary(101, New Date(2026, 11, 1), New String("r"c, 201), lines, Components).Succeeded)
    End Sub

    <Theory>
    <InlineData(1, 2, 26, 25, True)>
    <InlineData(26, 2, 26, 25, True)>
    <InlineData(15, 2, 26, 25, False)>
    <InlineData(1, 0.5, 26, 25, False)>
    <InlineData(1, 11, 26, 25, False)>
    <InlineData(1, 2, 0, 25, False)>
    <InlineData(1, 2, 32, 25, False)>
    <InlineData(1, 2, 26, -1, False)>
    <InlineData(1, 2, 26, 101, False)>
    <InlineData(1, 2, 26, 0, True)>
    Public Sub Policy_Limits(cycle As Integer, ot As Decimal, divisor As Decimal, emi As Decimal, expected As Boolean)
        Dim policy As New PolicySettings(cycle, ot, False, False, divisor, emi)
        Assert.Equal(expected, SalaryValidator.ValidatePolicy(policy).Succeeded)
    End Sub

    <Theory>
    <InlineData("BASIC", "Basic", False, False, True)>
    <InlineData("", "Basic", False, False, False)>
    <InlineData("BASIC", " ", False, False, False)>
    <InlineData("BASIC", "Basic", True, True, False)>
    <InlineData("ABCDEFGHIJKLMNOPQRSTU", "Basic", False, False, False)>
    Public Sub Component_Rules(code As String, name As String, isBasic As Boolean, isDa As Boolean, expected As Boolean)
        Dim c As New SalaryComponentItem(Nothing, code, name, isBasic, isDa, False, 10, True)
        Assert.Equal(expected, SalaryValidator.ValidateComponent(c).Succeeded)
    End Sub
End Class
