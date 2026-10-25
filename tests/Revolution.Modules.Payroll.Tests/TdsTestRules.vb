Imports Revolution.Core

''' <summary>Test fixtures shaped like the FY 2026-27 rows. They are not product rates; the product reads them from the stat tables.</summary>
Friend NotInheritable Class TdsTestRules
    Private Sub New()
    End Sub

    Public Shared Function NewRegime() As TdsRule
        Dim slabs As New List(Of TdsSlab) From {
            New TdsSlab(1, 0D, 400000D, 0D), New TdsSlab(2, 400000D, 800000D, 5D), New TdsSlab(3, 800000D, 1200000D, 10D),
            New TdsSlab(4, 1200000D, 1600000D, 15D), New TdsSlab(5, 1600000D, 2000000D, 20D),
            New TdsSlab(6, 2000000D, 2400000D, 25D), New TdsSlab(7, 2400000D, Nothing, 30D)}
        Dim bands As New List(Of TdsSurchargeBand) From {
            New TdsSurchargeBand(1, 5000000D, 10000000D, 10D), New TdsSurchargeBand(2, 10000000D, 20000000D, 15D),
            New TdsSurchargeBand(3, 20000000D, Nothing, 25D)}
        Return New TdsRule(2026, TdsRegime.New, TdsAgeBand.General, 75000D, 1200000D, 60000D, 4D, True, "test", slabs, bands)
    End Function

    Public Shared Function OldRegime() As TdsRule
        Dim slabs As New List(Of TdsSlab) From {
            New TdsSlab(1, 0D, 250000D, 0D), New TdsSlab(2, 250000D, 500000D, 5D),
            New TdsSlab(3, 500000D, 1000000D, 20D), New TdsSlab(4, 1000000D, Nothing, 30D)}
        Return New TdsRule(2026, TdsRegime.Old, TdsAgeBand.General, 50000D, 500000D, 12500D, 4D, True, "test", slabs, Nothing)
    End Function

    Public Shared Function OldLimits() As IReadOnlyList(Of TdsDeductionLimit)
        Return New List(Of TdsDeductionLimit) From {
            New TdsDeductionLimit(2026, TdsRegime.Old, "80C", "", 150000D, "", True, Nothing, "test"),
            New TdsDeductionLimit(2026, TdsRegime.Old, "80D", "self", 25000D, "", True, Nothing, "test")}
    End Function
End Class
