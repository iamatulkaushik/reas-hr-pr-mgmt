Imports Revolution.Core
Imports Xunit

Public Class PlanFeatureMapTests
    <Fact>
    Public Sub EveryFeature_HasAMinimumTier()
        For Each f As PlanFeature In [Enum].GetValues(GetType(PlanFeature))
            Dim tier = PlanFeatureMap.MinimumTier(f)
            Assert.True([Enum].IsDefined(GetType(PlanTier), tier))
        Next
    End Sub

    <Fact>
    Public Sub Copper_IncludesBasicPayroll_NotEpfEsi()
        Assert.True(PlanFeatureMap.IsIncluded(PlanTier.Copper, PlanFeature.BasicPayroll))
        Assert.False(PlanFeatureMap.IsIncluded(PlanTier.Copper, PlanFeature.EpfEsi))
    End Sub

    <Fact>
    Public Sub HigherPlans_IncludeEverythingBelow()
        For Each f As PlanFeature In [Enum].GetValues(GetType(PlanFeature))
            Assert.True(PlanFeatureMap.IsIncluded(PlanTier.Platinum, f))
        Next
        Assert.True(PlanFeatureMap.IsIncluded(PlanTier.Gold, PlanFeature.EpfEsi))
        Assert.False(PlanFeatureMap.IsIncluded(PlanTier.Gold, PlanFeature.DualApproval))
    End Sub

    <Fact>
    Public Sub UnknownFeature_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() PlanFeatureMap.MinimumTier(CType(999, PlanFeature)))
    End Sub
End Class
