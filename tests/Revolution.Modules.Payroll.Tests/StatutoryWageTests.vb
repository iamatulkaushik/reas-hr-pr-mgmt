Imports Revolution.Modules.Payroll
Imports Xunit

Public Class StatutoryWageTests
    <Fact>
    Public Sub CodeWage_ExclusionsAboveHalf_WageIsHalf()
        Assert.Equal(20000D, StatutoryWage.Compute(16000D, 40000D, 24000D, StatutoryTestRates.CodeWage()))
    End Sub

    <Fact>
    Public Sub CodeWage_ExclusionsBelowHalf_WageIsRemainder()
        Assert.Equal(30000D, StatutoryWage.Compute(16000D, 40000D, 10000D, StatutoryTestRates.CodeWage()))
    End Sub

    <Fact>
    Public Sub CodeWage_FloorFromRow()
        Assert.Equal(24000D, StatutoryWage.Compute(0D, 40000D, 24000D, StatutoryTestRates.CodeWage(60D)))
    End Sub

    <Fact>
    Public Sub OldRule_UsesFlaggedWage()
        Assert.Equal(16000D, StatutoryWage.Compute(16000D, 40000D, 24000D, StatutoryTestRates.OldWage()))
    End Sub

    <Fact>
    Public Sub ExclusionsAboveTotal_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() StatutoryWage.Compute(0D, 100D, 101D, StatutoryTestRates.CodeWage()))
    End Sub

    <Fact>
    Public Sub NullRule_Throws()
        Assert.Throws(Of ArgumentNullException)(Function() StatutoryWage.Compute(0D, 1D, 0D, Nothing))
    End Sub
End Class
