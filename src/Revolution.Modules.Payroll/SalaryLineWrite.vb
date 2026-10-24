''' <summary>One line of a salary revision to save: a component and its monthly figure.</summary>
Public NotInheritable Class SalaryLineWrite
    Public Sub New(componentId As Integer, amount As Decimal)
        Me.ComponentId = componentId
        Me.Amount = amount
    End Sub

    Public ReadOnly Property ComponentId As Integer
    ''' <summary>Monthly figure.</summary>
    Public ReadOnly Property Amount As Decimal
End Class
