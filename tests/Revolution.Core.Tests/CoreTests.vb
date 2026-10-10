Imports Revolution.Core
Imports Xunit

Public Class CoreTests
    <Fact>
    Public Sub Plans_AreOrderedLowToHigh()
        Assert.True(PlanTier.Copper < PlanTier.Silver)
        Assert.True(PlanTier.Silver < PlanTier.Gold)
        Assert.True(PlanTier.Gold < PlanTier.Platinum)
    End Sub

    <Fact>
    Public Sub Roles_DeveloperIsHighestPrivilege()
        Assert.True(UserRole.Developer < UserRole.Owner)
        Assert.True(UserRole.Owner < UserRole.Associate)
        Assert.True(UserRole.Associate < UserRole.Operator)
    End Sub

    <Fact>
    Public Sub Fail_RequiresMessage()
        Assert.Throws(Of ArgumentException)(Function() OperationResult.Fail(" "))
    End Sub

    <Fact>
    Public Sub Ok_HasNoMessage()
        Dim r = OperationResult.Ok()
        Assert.True(r.Succeeded)
        Assert.Equal(String.Empty, r.Message)
    End Sub
End Class
