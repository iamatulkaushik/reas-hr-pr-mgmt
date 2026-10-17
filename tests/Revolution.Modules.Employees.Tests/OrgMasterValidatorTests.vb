Imports Revolution.Modules.Employees
Imports Xunit

Public Class OrgMasterValidatorTests
    Private Shared Function Factory() As FactoryItem
        Return New FactoryItem With {.Code = "F1", .Name = "Main plant", .StateId = 6}
    End Function

    <Fact>
    Public Sub Factory_Valid_HasNoProblem()
        Assert.Empty(OrgMasterValidator.Validate(Factory()))
    End Sub

    <Theory>
    <InlineData("")>
    <InlineData("  ")>
    <InlineData("TOOLONGCODE1")>
    <InlineData("A B")>
    <InlineData("A/1")>
    Public Sub Code_Bad_IsRefused(code As String)
        Dim f = Factory()
        f.Code = code
        Assert.Single(OrgMasterValidator.Validate(f))
    End Sub

    <Theory>
    <InlineData(0)>
    <InlineData(256)>
    Public Sub Factory_StateOutOfRange_IsRefused(stateId As Integer)
        Dim f = Factory()
        f.StateId = stateId
        Assert.Single(OrgMasterValidator.Validate(f))
    End Sub

    <Fact>
    Public Sub Factory_MaxWorkersZero_IsRefused()
        Dim f = Factory()
        f.MaxWorkers = 0
        Assert.Single(OrgMasterValidator.Validate(f))
    End Sub

    <Fact>
    Public Sub Factory_EsiSubCodeAbove17_IsRefused()
        Dim f = Factory()
        f.EsiSubCode = New String("1"c, 18)
        Assert.Single(OrgMasterValidator.Validate(f))
    End Sub

    <Fact>
    Public Sub Department_NeedsFactory()
        Dim d As New DepartmentItem With {.Code = "D1", .Name = "Stitching"}
        Assert.Single(OrgMasterValidator.Validate(d))
        d.FactoryId = 3
        Assert.Empty(OrgMasterValidator.Validate(d))
    End Sub

    <Theory>
    <InlineData(0, False)>
    <InlineData(1, True)>
    <InlineData(7, True)>
    <InlineData(8, False)>
    Public Sub Designation_CategoryMustBeOneToSeven(category As Integer, valid As Boolean)
        Dim d As New DesignationItem With {.Code = "T1", .Name = "Tailor", .CategoryId = category}
        Assert.Equal(valid, OrgMasterValidator.Validate(d).Count = 0)
    End Sub

    <Fact>
    Public Sub EveryProblemIsListed()
        Dim d As New DesignationItem()
        Assert.Equal(3, OrgMasterValidator.Validate(d).Count)
    End Sub
End Class
