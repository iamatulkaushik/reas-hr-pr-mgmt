''' <summary>The bonus run that was made: its id and number, how many employees it pays and the total. The audit text holds no amounts.</summary>
Public NotInheritable Class BonusRunResult
    Public Sub New(runId As Integer, runNo As Integer, employees As Integer, total As Decimal)
        Me.RunId = runId
        Me.RunNo = runNo
        Me.Employees = employees
        Me.Total = total
    End Sub

    Public ReadOnly Property RunId As Integer
    Public ReadOnly Property RunNo As Integer
    Public ReadOnly Property Employees As Integer
    Public ReadOnly Property Total As Decimal
End Class
