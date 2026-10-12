Imports Revolution.Modules.Employees
Imports Xunit

Public Class PlacementValidatorTests
    Private Shared Function Valid() As EmployeePlacement
        Return EmployeeTestData.Placement()
    End Function

    <Fact>
    Public Sub ValidPlacement_HasNoProblems()
        Assert.Empty(PlacementValidator.Validate(Valid()))
    End Sub

    <Theory>
    <InlineData("")>
    <InlineData("X")>
    <InlineData("MF")>
    Public Sub Gender_MustBeMFO(gender As String)
        Dim p = Valid()
        p.Gender = gender
        Assert.Contains("gender", String.Join(" ", PlacementValidator.Validate(p)), StringComparison.OrdinalIgnoreCase)
    End Sub

    <Fact>
    Public Sub Gender_LowerCaseIsAccepted()
        Dim p = Valid()
        p.Gender = "f"
        Assert.Empty(PlacementValidator.Validate(p))
    End Sub

    <Theory>
    <InlineData(0)>
    <InlineData(7)>
    Public Sub EmploymentType_OutOfRange_IsRefused(value As Integer)
        Dim p = Valid()
        p.EmploymentTypeId = value
        Assert.Single(PlacementValidator.Validate(p))
    End Sub

    <Fact>
    Public Sub Factory_Missing_IsRefused()
        Dim p = Valid()
        p.FactoryId = 0
        Assert.Single(PlacementValidator.Validate(p))
    End Sub

    <Theory>
    <InlineData("a@b.in", True)>
    <InlineData("", True)>
    <InlineData("a b@c.in", False)>
    <InlineData("a@@c.in", False)>
    <InlineData("@c.in", False)>
    <InlineData("a@", False)>
    Public Sub Email_Plausible(text As String, expected As Boolean)
        Dim p = Valid()
        p.Email = text
        Assert.Equal(expected, PlacementValidator.Validate(p).Count = 0)
    End Sub

    <Fact>
    Public Sub MaritalStatus_OutOfRange_IsRefused()
        Dim p = Valid()
        p.MaritalStatusId = 5
        Assert.Single(PlacementValidator.Validate(p))
    End Sub
End Class
