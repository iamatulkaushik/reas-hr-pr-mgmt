Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class RupeeTests
    <Fact>
    Public Sub Nearest_MidpointGoesAwayFromZero()
        Assert.Equal(1250D, Rupee.Round(1249.5D, RateRounding.Nearest))
    End Sub

    <Fact>
    Public Sub Up_RoundsToNextRupee()
        Assert.Equal(158D, Rupee.Round(157.01D, RateRounding.Up))
    End Sub

    <Fact>
    Public Sub Up_WholeAmountStays()
        Assert.Equal(135D, Rupee.Round(135D, RateRounding.Up))
    End Sub

    <Fact>
    Public Sub Down_DropsFraction()
        Assert.Equal(157D, Rupee.Round(157.99D, RateRounding.Down))
    End Sub

    <Fact>
    Public Sub Negative_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Function() Rupee.Round(-1D, RateRounding.Nearest))
    End Sub
End Class
