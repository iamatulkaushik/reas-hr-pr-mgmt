Imports Revolution.Modules.Employees
Imports Xunit

Public Class FamilyValidatorTests
    Private Shared ReadOnly Today As New Date(2026, 10, 17)

    Private Shared Function Member() As FamilyMember
        Return New FamilyMember With {.Name = "Sunita", .Relation = "Spouse"}
    End Function

    <Fact>
    Public Sub Member_Valid_HasNoProblem()
        Assert.Empty(FamilyValidator.Validate(Member(), Today))
    End Sub

    <Fact>
    Public Sub Member_NameAndRelationRequired()
        Assert.Equal(2, FamilyValidator.Validate(New FamilyMember(), Today).Count)
    End Sub

    <Fact>
    Public Sub Member_NameAbove120_IsRefused()
        Dim m = Member()
        m.Name = New String("a"c, 121)
        Assert.Single(FamilyValidator.Validate(m, Today))
    End Sub

    <Fact>
    Public Sub Member_FutureBirthDate_IsRefused()
        Dim m = Member()
        m.DateOfBirth = Today.AddDays(1)
        Assert.Single(FamilyValidator.Validate(m, Today))
        m.DateOfBirth = Today
        Assert.Empty(FamilyValidator.Validate(m, Today))
    End Sub

    <Theory>
    <InlineData(0, False)>
    <InlineData(0.01, True)>
    <InlineData(50, True)>
    <InlineData(100, True)>
    <InlineData(100.01, False)>
    <InlineData(33.333, False)>
    Public Sub Nomination_ShareRules(share As Double, valid As Boolean)
        Dim problems = FamilyValidator.ValidateNomination(NomineeScheme.Epf, 5, CDec(share))
        Assert.Equal(valid, problems.Count = 0)
    End Sub

    <Fact>
    Public Sub Nomination_NeedsSchemeAndMember()
        Assert.Equal(2, FamilyValidator.ValidateNomination(CType(9, NomineeScheme), 0, 50D).Count)
    End Sub
End Class
