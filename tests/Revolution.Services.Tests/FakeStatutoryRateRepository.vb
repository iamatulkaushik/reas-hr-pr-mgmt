Imports Revolution.Core

Friend NotInheritable Class FakeStatutoryRateRepository
    Implements IStatutoryRateRepository

    Public Property Epf As EpfRate
    Public Property Esi As EsiRate
    Public Property Pt As PtRule
    Public Property Wage As WageRule
    Public Property Gratuity As GratuityRule
    Public Property Bonus As BonusRule
    Public Property DeductionLimits As IReadOnlyList(Of TdsDeductionLimit) = New List(Of TdsDeductionLimit)()
    Public Property HraCities As IReadOnlyList(Of HraCity) = New List(Of HraCity)()
    Public Property Tds As TdsRule
    Public Property LastFinancialYear As Integer
    Public Property LastRegime As TdsRegime
    Public Property LastAgeBand As TdsAgeBand
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

    Public Function GetGratuityRule(asOf As Date) As GratuityRule Implements IStatutoryRateRepository.GetGratuityRule
        LastAsOf = asOf
        Return Gratuity
    End Function

    Public Function GetBonusRule(asOf As Date) As BonusRule Implements IStatutoryRateRepository.GetBonusRule
        LastAsOf = asOf
        Return Bonus
    End Function

    Public Function GetTdsDeductionLimits(financialYear As Integer, regime As TdsRegime) As IReadOnlyList(Of TdsDeductionLimit) Implements IStatutoryRateRepository.GetTdsDeductionLimits
        LastFinancialYear = financialYear
        LastRegime = regime
        Return DeductionLimits
    End Function

    Public Function GetHraCities(asOf As Date) As IReadOnlyList(Of HraCity) Implements IStatutoryRateRepository.GetHraCities
        LastAsOf = asOf
        Return HraCities
    End Function

    Public Function GetTdsRule(financialYear As Integer, regime As TdsRegime, ageBand As TdsAgeBand) As TdsRule Implements IStatutoryRateRepository.GetTdsRule
        LastFinancialYear = financialYear
        LastRegime = regime
        LastAgeBand = ageBand
        Return Tds
    End Function

    Public Property RuleValue As TdsRuleValue

    Public Function GetTdsRuleValue(valueKey As String, financialYear As Integer) As TdsRuleValue Implements IStatutoryRateRepository.GetTdsRuleValue
        LastKey = valueKey
        LastFinancialYear = financialYear
        Return RuleValue
    End Function

    Public Property EmployerNps As TdsEmployerNps

    Public Function GetTdsEmployerNps(financialYear As Integer, regime As TdsRegime) As TdsEmployerNps Implements IStatutoryRateRepository.GetTdsEmployerNps
        LastFinancialYear = financialYear
        LastRegime = regime
        Return EmployerNps
    End Function

    Public Property TaxForm As TaxReference
    Public Property TaxSectionRef As TaxReference
    Public Property LastKey As String

    Public Function GetTaxForm(purposeKey As String, financialYear As Integer) As TaxReference Implements IStatutoryRateRepository.GetTaxForm
        LastKey = purposeKey
        LastFinancialYear = financialYear
        Return TaxForm
    End Function

    Public Function GetTaxSectionRef(topicKey As String, financialYear As Integer) As TaxReference Implements IStatutoryRateRepository.GetTaxSectionRef
        LastKey = topicKey
        LastFinancialYear = financialYear
        Return TaxSectionRef
    End Function

    Public Function IsVerificationRequired() As Boolean Implements IStatutoryRateRepository.IsVerificationRequired
        Return VerificationRequired
    End Function

    Friend Shared Function EpfRow(verified As Boolean) As EpfRate
        Return New EpfRate(New Date(2025, 4, 1), 12D, 3.67D, 8.33D, 0.5D, 0.5D, 500D, 15000D, 15000D, verified, "test")
    End Function

    Friend Shared Function TdsRow(verified As Boolean, slabCount As Integer) As TdsRule
        Dim slabs As New List(Of TdsSlab)()
        For i = 1 To slabCount
            slabs.Add(New TdsSlab(i, (i - 1) * 100000D, Nothing, i * 5D))
        Next
        Return New TdsRule(2026, TdsRegime.New, TdsAgeBand.General, 75000D, 1200000D, 60000D, 4D, verified, "test", slabs, Nothing)
    End Function

    Friend Shared Function PtRow(verified As Boolean, slabCount As Integer) As PtRule
        Dim slabs As New List(Of PtSlab)()
        For i = 1 To slabCount
            slabs.Add(New PtSlab(0, i, i * 1000D, Nothing, 100D, Nothing))
        Next
        Return New PtRule(27, New Date(2025, 4, 1), PtFrequency.Monthly, 2, verified, "test", slabs)
    End Function

    Public Property Lwf As LabourWelfareRule
    Public Property MinimumWages As IReadOnlyList(Of MinimumWageRate) = New List(Of MinimumWageRate)()

    Public Function GetLabourWelfareRule(stateId As Integer, asOf As Date) As LabourWelfareRule Implements IStatutoryRateRepository.GetLabourWelfareRule
        LastAsOf = asOf
        LastStateId = stateId
        Return Lwf
    End Function

    Public Function GetMinimumWages(stateId As Integer, asOf As Date) As IReadOnlyList(Of MinimumWageRate) Implements IStatutoryRateRepository.GetMinimumWages
        LastAsOf = asOf
        LastStateId = stateId
        Return MinimumWages
    End Function
End Class
