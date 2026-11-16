''' <summary>One part of the amount of a pack line, for example the employee share of the provident fund.</summary>
Public NotInheritable Class PackAmount
    Public Sub New(label As String, amount As Decimal)
        Me.Label = If(label, String.Empty)
        Me.Amount = amount
    End Sub

    Public ReadOnly Property Label As String
    Public ReadOnly Property Amount As Decimal
End Class
