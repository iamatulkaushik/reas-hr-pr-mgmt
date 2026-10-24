''' <summary>One run in the run picker of the export screens. Period, number, status and head count. No money.</summary>
Public NotInheritable Class PayrollRunItem
    Public Sub New(runId As Integer, periodFrom As Date, periodTo As Date, runNo As Integer, status As PayrollRunStatus, employeeCount As Integer)
        Me.RunId = runId
        Me.PeriodFrom = periodFrom
        Me.PeriodTo = periodTo
        Me.RunNo = runNo
        Me.Status = status
        Me.EmployeeCount = employeeCount
    End Sub

    Public ReadOnly Property RunId As Integer
    Public ReadOnly Property PeriodFrom As Date
    Public ReadOnly Property PeriodTo As Date
    Public ReadOnly Property RunNo As Integer
    Public ReadOnly Property Status As PayrollRunStatus
    Public ReadOnly Property EmployeeCount As Integer

    ''' <summary>Text for the picker, for example 26-09-2026 to 25-10-2026, run 1, Approved, 120 employees.</summary>
    Public ReadOnly Property Label As String
        Get
            Return $"{PeriodFrom:dd-MM-yyyy} to {PeriodTo:dd-MM-yyyy}, run {RunNo}, {Status}, {EmployeeCount} employees"
        End Get
    End Property
End Class
