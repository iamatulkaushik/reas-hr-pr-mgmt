''' <summary>Bonus at exit. When not eligible, Amount is 0 and Reason says why. WageBase is the sum of the monthly wages after the calculation ceiling.</summary>
Public NotInheritable Class BonusResult
    Public Sub New(isEligible As Boolean, reason As String, wageBase As Decimal, pct As Decimal, grossBonus As Decimal, alreadyPaid As Decimal, amount As Decimal)
        Me.IsEligible = isEligible
        Me.Reason = If(reason, String.Empty)
        Me.WageBase = wageBase
        Me.Pct = pct
        Me.GrossBonus = grossBonus
        Me.AlreadyPaid = alreadyPaid
        Me.Amount = amount
    End Sub

    Public ReadOnly Property IsEligible As Boolean
    Public ReadOnly Property Reason As String
    Public ReadOnly Property WageBase As Decimal
    Public ReadOnly Property Pct As Decimal
    ''' <summary>Bonus for the part year before taking off what was already paid.</summary>
    Public ReadOnly Property GrossBonus As Decimal
    Public ReadOnly Property AlreadyPaid As Decimal
    ''' <summary>Whole rupees payable in F&amp;F, never below zero.</summary>
    Public ReadOnly Property Amount As Decimal
End Class
