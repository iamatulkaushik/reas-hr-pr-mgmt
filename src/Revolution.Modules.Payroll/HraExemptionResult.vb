''' <summary>Yearly HRA exemption in whole rupees, with the month lines for the Form 124 and Form 130 pages.</summary>
Public NotInheritable Class HraExemptionResult
    Public Sub New(totalExempt As Decimal, months As IReadOnlyList(Of HraMonthResult))
        Me.TotalExempt = totalExempt
        Me.Months = months
    End Sub

    Public ReadOnly Property TotalExempt As Decimal
    Public ReadOnly Property Months As IReadOnlyList(Of HraMonthResult)
End Class
