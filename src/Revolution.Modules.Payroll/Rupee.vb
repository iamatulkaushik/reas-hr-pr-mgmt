Imports Revolution.Core

''' <summary>Rounds money to a whole rupee. Decimal only, never Double. Midpoint goes away from zero.</summary>
Public NotInheritable Class Rupee
    Private Sub New()
    End Sub

    Public Shared Function Percent(amount As Decimal, pct As Decimal) As Decimal
        Return amount * pct / 100D
    End Function

    Public Shared Function Round(amount As Decimal, rounding As RateRounding) As Decimal
        If amount < 0D Then
            Throw New ArgumentOutOfRangeException(NameOf(amount), "Amount cannot be negative.")
        End If
        Select Case rounding
            Case RateRounding.Up
                Return Math.Ceiling(amount)
            Case RateRounding.Down
                Return Math.Floor(amount)
            Case RateRounding.Nearest
                Return Math.Round(amount, 0, MidpointRounding.AwayFromZero)
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(rounding))
        End Select
    End Function
End Class
