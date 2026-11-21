''' <summary>The amount and the employee count of one state for the Monthly Pack (professional tax).</summary>
Public NotInheritable Class PackStateAmount
    Public Sub New(stateId As Integer, stateName As String, amount As Decimal, employeeCount As Integer)
        Me.StateId = stateId
        Me.StateName = If(stateName, String.Empty)
        Me.Amount = amount
        Me.EmployeeCount = employeeCount
    End Sub

    Public ReadOnly Property StateId As Integer
    Public ReadOnly Property StateName As String
    Public ReadOnly Property Amount As Decimal
    Public ReadOnly Property EmployeeCount As Integer
End Class
