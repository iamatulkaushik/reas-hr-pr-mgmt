''' <summary>Lists recent runs of the current company through pay.usp_Run_List (ADR 0008). Newest period first.</summary>
Public Interface IPayrollRunLister
    Function ListRuns() As IReadOnlyList(Of PayrollRunItem)
End Interface
