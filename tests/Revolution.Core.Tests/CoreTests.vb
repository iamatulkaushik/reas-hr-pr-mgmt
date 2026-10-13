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

    <Fact>
    Public Sub FinancialYear_RunsAprilToMarch()
        Assert.Equal(2026, FinancialYearCalendar.StartYear(New Date(2026, 4, 1)))
        Assert.Equal(2026, FinancialYearCalendar.StartYear(New Date(2027, 3, 31)))
        Assert.Equal(2025, FinancialYearCalendar.StartYear(New Date(2026, 3, 31)))
    End Sub

    <Fact>
    Public Sub FinancialYear_Label_UsesTwoDigitEndYear()
        Assert.Equal("2026-27", FinancialYearCalendar.Label(2026))
        Assert.Equal("2099-00", FinancialYearCalendar.Label(2099))
    End Sub
End Class
