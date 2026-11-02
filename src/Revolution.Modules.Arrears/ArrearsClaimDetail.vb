''' <summary>A claim with its month rows.</summary>
Public NotInheritable Class ArrearsClaimDetail
    Public Sub New(claim As ArrearsClaimRow, months As IReadOnlyList(Of ArrearsMonthRecord))
        If claim Is Nothing Then Throw New ArgumentNullException(NameOf(claim))
        If months Is Nothing Then Throw New ArgumentNullException(NameOf(months))
        Me.Claim = claim
        Me.Months = months
    End Sub

    Public ReadOnly Property Claim As ArrearsClaimRow
    Public ReadOnly Property Months As IReadOnlyList(Of ArrearsMonthRecord)
End Class
