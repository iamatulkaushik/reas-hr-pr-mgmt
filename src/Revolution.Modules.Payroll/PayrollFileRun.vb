''' <summary>Run header as the file source needs it: period and status. No money, no ids.</summary>
Public NotInheritable Class PayrollFileRun
    Public Sub New(runId As Integer, periodFrom As Date, periodTo As Date, status As PayrollRunStatus)
        Me.RunId = runId
        Me.PeriodFrom = periodFrom
        Me.PeriodTo = periodTo
        Me.Status = status
    End Sub

    Public ReadOnly Property RunId As Integer
    Public ReadOnly Property PeriodFrom As Date
    Public ReadOnly Property PeriodTo As Date
    Public ReadOnly Property Status As PayrollRunStatus
End Class
