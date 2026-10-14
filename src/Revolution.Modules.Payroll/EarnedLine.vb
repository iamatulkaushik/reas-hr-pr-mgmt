''' <summary>Amount earned on one head for the period, in whole rupees.</summary>
Public NotInheritable Class EarnedLine
    Public Sub New(code As String, amount As Decimal)
        Me.Code = code
        Me.Amount = amount
    End Sub

    Public ReadOnly Property Code As String
    Public ReadOnly Property Amount As Decimal
End Class
