''' <summary>
''' Value of an asset on a day: straight line over the type's useful life, whole months only (a month counts when the day of the month has come round again), floor of zero, whole rupees.
''' Used for the default amount of a recovery (Owner answer, 2026-11-11: recovery at depreciated value).
''' </summary>
Public NotInheritable Class AssetDepreciation
    Private Sub New()
    End Sub

    ''' <summary>Whole months from the purchase date to asOf. Zero when asOf is on or before the purchase date.</summary>
    Public Shared Function CompleteMonths(purchaseDate As Date, asOf As Date) As Integer
        If asOf.Date <= purchaseDate.Date Then Return 0
        Dim months As Integer = (asOf.Year - purchaseDate.Year) * 12 + asOf.Month - purchaseDate.Month
        If asOf.Day < purchaseDate.Day Then months -= 1
        Return Math.Max(months, 0)
    End Function

    ''' <summary>The depreciated value on asOf, in whole rupees: the purchase value less the months used, in a straight line, never below zero.</summary>
    Public Shared Function ValueOn(purchaseValue As Decimal, purchaseDate As Date, usefulLifeMonths As Integer, asOf As Date) As Decimal
        If usefulLifeMonths < 1 Then Throw New ArgumentOutOfRangeException(NameOf(usefulLifeMonths), "Useful life must be at least one month.")
        If purchaseValue <= 0D Then Return 0D
        Dim months As Integer = CompleteMonths(purchaseDate, asOf)
        If months >= usefulLifeMonths Then Return 0D
        Return Math.Round(purchaseValue * (usefulLifeMonths - months) / usefulLifeMonths, 0, MidpointRounding.AwayFromZero)
    End Function
End Class
