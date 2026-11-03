''' <summary>The difference on one salary head for one wage month, signed whole rupees. Heads are stored and paid one by one (Owner, 2026-11-03).</summary>
Public NotInheritable Class ArrearsHeadAmount
    Public Sub New(componentId As Integer, amount As Decimal)
        Me.ComponentId = componentId
        Me.Amount = amount
    End Sub

    Public ReadOnly Property ComponentId As Integer
    Public ReadOnly Property Amount As Decimal
End Class
