Imports Revolution.Core

''' <summary>The four statutory lookups an arrears claim needs, for the rates of a wage month. HostArrearsRates answers them from the shared statutory rate service.</summary>
Public Interface IArrearsRates
    Function GetEpfRate(asOf As Date) As OperationResult(Of EpfRate)
    Function GetEsiRate(asOf As Date) As OperationResult(Of EsiRate)
    Function GetWageRule(asOf As Date) As OperationResult(Of WageRule)
    Function GetPtRule(stateId As Integer, asOf As Date) As OperationResult(Of PtRule)
End Interface
