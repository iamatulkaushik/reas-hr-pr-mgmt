''' <summary>Gratuity for one exit. Amount is in whole rupees. Reason says why nothing is payable.</summary>
Public NotInheritable Class GratuityResult
    Public Sub New(eligible As Boolean, completedYears As Integer, countedYears As Integer, amount As Decimal, capApplied As Boolean, reason As String)
        Me.Eligible = eligible
        Me.CompletedYears = completedYears
        Me.CountedYears = countedYears
        Me.Amount = amount
        Me.CapApplied = capApplied
        Me.Reason = reason
    End Sub

    Public ReadOnly Property Eligible As Boolean
    Public ReadOnly Property CompletedYears As Integer
    ''' <summary>Completed years, plus one when the part year is above the round-up months.</summary>
    Public ReadOnly Property CountedYears As Integer
    Public ReadOnly Property Amount As Decimal
    Public ReadOnly Property CapApplied As Boolean
    Public ReadOnly Property Reason As String
End Class
