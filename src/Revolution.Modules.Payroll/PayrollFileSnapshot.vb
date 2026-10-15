''' <summary>A payroll run as the file service needs it: period, status and one entry per employee with ids already decrypted.</summary>
Public NotInheritable Class PayrollFileSnapshot
    Public Sub New(runId As Integer, periodFrom As Date, periodTo As Date, status As PayrollRunStatus, employees As IReadOnlyList(Of PayrollFileEmployee))
        Me.RunId = runId
        Me.PeriodFrom = periodFrom
        Me.PeriodTo = periodTo
        Me.Status = status
        Me.Employees = employees
    End Sub

    Public ReadOnly Property RunId As Integer
    Public ReadOnly Property PeriodFrom As Date
    Public ReadOnly Property PeriodTo As Date
    Public ReadOnly Property Status As PayrollRunStatus
    Public ReadOnly Property Employees As IReadOnlyList(Of PayrollFileEmployee)
End Class
