''' <summary>Lists the runs of a pay period, every status, regular run first (pay.usp_Run_ListForPeriod). Optional on PayrollRunService: without it the Lock Payroll screen cannot list runs.</summary>
Public Interface IRunDirectory
    Function ListForPeriod(period As RunPeriod) As IReadOnlyList(Of PayrollRunHeader)
End Interface
