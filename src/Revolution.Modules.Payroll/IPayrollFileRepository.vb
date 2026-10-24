''' <summary>Reads a run for file export through pay stored procedures (ADR 0008). Never returns or decrypts an id.</summary>
Public Interface IPayrollFileRepository
    ''' <summary>Nothing when the run is not in the current company.</summary>
    Function GetRun(runId As Integer) As PayrollFileRun

    Function GetRows(runId As Integer) As IReadOnlyList(Of PayrollFileRow)
End Interface
