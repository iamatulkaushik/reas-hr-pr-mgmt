Imports Revolution.Core
Imports Xunit

Public Class OperationResultOfTTests
    <Fact>
    Public Sub Ok_CarriesValue()
        Dim r = OperationResult(Of Integer).Ok(42)
        Assert.True(r.Succeeded)
        Assert.Equal(42, r.Value)
    End Sub

    <Fact>
    Public Sub Fail_RequiresMessage()
        Assert.Throws(Of ArgumentException)(Function() OperationResult(Of Integer).Fail(""))
    End Sub

    <Fact>
    Public Sub UserSession_RequiresUserName()
        Assert.Throws(Of ArgumentException)(Function() New UserSession(1, " ", UserRole.Operator, 1, PlanTier.Copper))
    End Sub
End Class
