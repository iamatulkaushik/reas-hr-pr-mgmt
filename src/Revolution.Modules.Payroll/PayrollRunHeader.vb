''' <summary>A payroll run as stored: period, status and totals (whole rupees as stored by the database).</summary>
Public NotInheritable Class PayrollRunHeader
    Public Sub New(runId As Integer, periodFrom As Date, periodTo As Date, runNo As Integer, status As PayrollRunStatus, employeeCount As Integer,
                   totalGross As Decimal, totalDeductions As Decimal, totalNet As Decimal)
        Me.RunId = runId
        Me.PeriodFrom = periodFrom
        Me.PeriodTo = periodTo
        Me.RunNo = runNo
        Me.Status = status
        Me.EmployeeCount = employeeCount
        Me.TotalGross = totalGross
        Me.TotalDeductions = totalDeductions
        Me.TotalNet = totalNet
    End Sub

    Public ReadOnly Property RunId As Integer
    Public ReadOnly Property PeriodFrom As Date
    Public ReadOnly Property PeriodTo As Date
    Public ReadOnly Property RunNo As Integer
    Public ReadOnly Property Status As PayrollRunStatus
    Public ReadOnly Property EmployeeCount As Integer
    Public ReadOnly Property TotalGross As Decimal
    Public ReadOnly Property TotalDeductions As Decimal
    Public ReadOnly Property TotalNet As Decimal

    Public Function Period() As RunPeriod
        Return New RunPeriod(PeriodFrom, PeriodTo)
    End Function
End Class
