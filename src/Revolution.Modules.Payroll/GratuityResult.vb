''' <summary>The gratuity for one employee at exit. When not eligible, Amount is 0 and Reason says why.</summary>
Public NotInheritable Class GratuityResult
    Public Sub New(isEligible As Boolean, reason As String, completedYears As Integer, extraMonths As Integer, countedYears As Integer,
                   amount As Decimal, wasCapped As Boolean)
        Me.IsEligible = isEligible
        Me.Reason = If(reason, String.Empty)
        Me.CompletedYears = completedYears
        Me.ExtraMonths = extraMonths
        Me.CountedYears = countedYears
        Me.Amount = amount
        Me.WasCapped = wasCapped
    End Sub

    Public ReadOnly Property IsEligible As Boolean
    Public ReadOnly Property Reason As String
    ''' <summary>Whole years of service.</summary>
    Public ReadOnly Property CompletedYears As Integer
    ''' <summary>Whole months beyond the completed years.</summary>
    Public ReadOnly Property ExtraMonths As Integer
    ''' <summary>Years the amount is worked on: the completed years, plus one when the extra months are more than the rule's limit.</summary>
    Public ReadOnly Property CountedYears As Integer
    ''' <summary>Whole rupees, after the cap.</summary>
    Public ReadOnly Property Amount As Decimal
    Public ReadOnly Property WasCapped As Boolean
End Class
