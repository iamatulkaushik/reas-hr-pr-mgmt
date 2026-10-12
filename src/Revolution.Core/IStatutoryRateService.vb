''' <summary>
''' Rates and rules for payroll. A missing row or (while verification is required) a row the CA has not signed off
''' returns a failure with a message the screen can show. Payroll must stop on a failure, never guess a rate.
''' </summary>
Public Interface IStatutoryRateService
    Function GetEpfRate(asOf As Date) As OperationResult(Of EpfRate)
    Function GetEsiRate(asOf As Date) As OperationResult(Of EsiRate)
    Function GetPtRule(stateId As Integer, asOf As Date) As OperationResult(Of PtRule)
    Function GetWageRule(asOf As Date) As OperationResult(Of WageRule)
End Interface
