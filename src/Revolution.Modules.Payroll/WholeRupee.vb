''' <summary>Rounds to the nearest whole rupee, halves away from zero, for statutory files.</summary>
Friend NotInheritable Class WholeRupee
    Private Sub New()
    End Sub

    Public Shared Function Round(amount As Decimal) As Decimal
        Return Decimal.Round(amount, 0, MidpointRounding.AwayFromZero)
    End Function
End Class
