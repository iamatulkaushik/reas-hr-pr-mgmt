''' <summary>Amount earned on one salary component in a run line, for the payslip.</summary>
Public NotInheritable Class EarningAmount
    Public Sub New(componentId As Integer, amount As Decimal)
        Me.ComponentId = componentId
        Me.Amount = amount
    End Sub

    Public ReadOnly Property ComponentId As Integer
    Public ReadOnly Property Amount As Decimal
End Class
