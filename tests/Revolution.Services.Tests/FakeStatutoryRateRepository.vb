Imports Revolution.Core

Friend NotInheritable Class FakeStatutoryRateRepository
    Implements IStatutoryRateRepository

    Public Property Epf As EpfRate
    Public Property Esi As EsiRate
    Public Property Pt As PtRule
    Public Property Wage As WageRule
    Public Property VerificationRequired As Boolean = True
    Public Property LastAsOf As Date
    Public Property LastStateId As Integer

    Public Function GetEpfRate(asOf As Date) As EpfRate Implements IStatutoryRateRepository.GetEpfRate
        LastAsOf = asOf
        Return Epf
    End Function

    Public Function GetEsiRate(asOf As Date) As EsiRate Implements IStatutoryRateRepository.GetEsiRate
        LastAsOf = asOf
        Return Esi
    End Function

    Public Function GetPtRule(stateId As Integer, asOf As Date) As PtRule Implements IStatutoryRateRepository.GetPtRule
        LastAsOf = asOf
        LastStateId = stateId
        Return Pt
    End Function

    Public Function GetWageRule(asOf As Date) As WageRule Implements IStatutoryRateRepository.GetWageRule
        LastAsOf = asOf
        Return Wage
    End Function

    Public Function IsVerificationRequired() As Boolean Implements IStatutoryRateRepository.IsVerificationRequired
        Return VerificationRequired
    End Function

    Friend Shared Function EpfRow(verified As Boolean) As EpfRate
        Return New EpfRate(New Date(2025, 4, 1), 12D, 3.67D, 8.33D, 0.5D, 0.5D, 500D, 15000D, 15000D, verified, "test")
    End Function

    Friend Shared Function PtRow(verified As Boolean, slabCount As Integer) As PtRule
        Dim slabs As New List(Of PtSlab)()
        For i = 1 To slabCount
            slabs.Add(New PtSlab(0, i, i * 1000D, Nothing, 100D, Nothing))
        Next
        Return New PtRule(27, New Date(2025, 4, 1), PtFrequency.Monthly, 2, verified, "test", slabs)
    End Function
End Class
