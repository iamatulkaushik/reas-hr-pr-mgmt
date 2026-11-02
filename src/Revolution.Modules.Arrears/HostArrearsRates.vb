Imports Revolution.Core

''' <summary>Answers IArrearsRates from the shared statutory rate service (rates come only from there, after CA sign-off).</summary>
Public NotInheritable Class HostArrearsRates
    Implements IArrearsRates

    Private ReadOnly _rates As IStatutoryRateService

    Public Sub New(rates As IStatutoryRateService)
        If rates Is Nothing Then Throw New ArgumentNullException(NameOf(rates))
        _rates = rates
    End Sub

    Public Function GetEpfRate(asOf As Date) As OperationResult(Of EpfRate) Implements IArrearsRates.GetEpfRate
        Return _rates.GetEpfRate(asOf)
    End Function

    Public Function GetEsiRate(asOf As Date) As OperationResult(Of EsiRate) Implements IArrearsRates.GetEsiRate
        Return _rates.GetEsiRate(asOf)
    End Function

    Public Function GetWageRule(asOf As Date) As OperationResult(Of WageRule) Implements IArrearsRates.GetWageRule
        Return _rates.GetWageRule(asOf)
    End Function

    Public Function GetPtRule(stateId As Integer, asOf As Date) As OperationResult(Of PtRule) Implements IArrearsRates.GetPtRule
        Return _rates.GetPtRule(stateId, asOf)
    End Function
End Class
