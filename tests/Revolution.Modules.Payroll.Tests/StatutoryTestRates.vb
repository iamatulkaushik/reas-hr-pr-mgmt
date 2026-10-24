Imports Revolution.Core

''' <summary>Rate rows for tests only. These are fixtures, not the rates the product uses. The product reads rates from stat.* after CA sign-off.</summary>
Friend NotInheritable Class StatutoryTestRates
    Private Sub New()
    End Sub

    Friend Shared ReadOnly Start As New Date(2026, 4, 1)

    Friend Shared Function Epf() As EpfRate
        Return New EpfRate(Start, 12D, 3.67D, 8.33D, 0.5D, 0.5D, 0D, 15000D, 15000D, True, "test")
    End Function

    Friend Shared Function Esi(Optional rounding As RateRounding = RateRounding.Up, Optional dailyExemption As Decimal = 0D) As EsiRate
        Return New EsiRate(Start, 0.75D, 3.25D, 21000D, dailyExemption, rounding, True, "test")
    End Function

    Friend Shared Function CodeWage(Optional floorPct As Decimal = 50D) As WageRule
        Return New WageRule(Start, True, floorPct, "test", True, "test")
    End Function

    Friend Shared Function OldWage() As WageRule
        Return New WageRule(Start, False, 0D, "test", True, "test")
    End Function

    Friend Shared Function Pt(frequency As PtFrequency, specialMonth As Integer?, Optional dueMonths As IReadOnlyList(Of Integer) = Nothing) As PtRule
        Dim slabs As New List(Of PtSlab) From {
            New PtSlab(0, 1, 0D, 10000D, 0D, Nothing),
            New PtSlab(0, 2, 10001D, 15000D, 150D, Nothing),
            New PtSlab(0, 3, 15001D, Nothing, 200D, 300D),
            New PtSlab(2, 1, 0D, 25000D, 0D, Nothing),
            New PtSlab(2, 2, 25001D, Nothing, 200D, 300D)
        }
        Return New PtRule(1, Start, frequency, specialMonth, True, "test", slabs, dueMonths)
    End Function
End Class
