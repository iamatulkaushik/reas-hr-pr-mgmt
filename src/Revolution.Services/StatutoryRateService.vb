Imports System.Globalization

''' <summary>Looks up effective-dated statutory rows and applies the CA sign-off rule (cfg.Setting RateVerificationRequired).</summary>
Public NotInheritable Class StatutoryRateService
    Implements IStatutoryRateService

    Private ReadOnly _repository As IStatutoryRateRepository

    Public Sub New(repository As IStatutoryRateRepository)
        If repository Is Nothing Then
            Throw New ArgumentNullException(NameOf(repository))
        End If
        _repository = repository
    End Sub

    Public Function GetEpfRate(asOf As Date) As OperationResult(Of EpfRate) Implements IStatutoryRateService.GetEpfRate
        Dim rate = _repository.GetEpfRate(asOf)
        Return Check(rate, rate?.Verified, "EPF rate", asOf, rate?.EffectiveFrom)
    End Function

    Public Function GetEsiRate(asOf As Date) As OperationResult(Of EsiRate) Implements IStatutoryRateService.GetEsiRate
        Dim rate = _repository.GetEsiRate(asOf)
        Return Check(rate, rate?.Verified, "ESI rate", asOf, rate?.EffectiveFrom)
    End Function

    Public Function GetPtRule(stateId As Integer, asOf As Date) As OperationResult(Of PtRule) Implements IStatutoryRateService.GetPtRule
        Dim rule = _repository.GetPtRule(stateId, asOf)
        Dim label = "professional tax rule for state " & stateId.ToString(CultureInfo.InvariantCulture)
        If rule IsNot Nothing AndAlso rule.Slabs.Count = 0 Then
            Return OperationResult(Of PtRule).Fail($"The {label} from {Format(rule.EffectiveFrom)} has no slabs. Ask the CA to complete it.")
        End If
        Dim needed = If(rule Is Nothing, 0, DueMonthsNeeded(rule.Frequency))
        If rule IsNot Nothing AndAlso needed > 0 AndAlso rule.DueMonths.Count <> needed Then
            Return OperationResult(Of PtRule).Fail($"The {label} from {Format(rule.EffectiveFrom)} is not monthly but does not list its {needed.ToString(CultureInfo.InvariantCulture)} due month(s). Ask the CA to complete it.")
        End If
        Return Check(rule, rule?.Verified, label, asOf, rule?.EffectiveFrom)
    End Function

    Public Function GetWageRule(asOf As Date) As OperationResult(Of WageRule) Implements IStatutoryRateService.GetWageRule
        Dim rule = _repository.GetWageRule(asOf)
        Return Check(rule, rule?.Verified, "wage rule", asOf, rule?.EffectiveFrom)
    End Function

    Public Function GetGratuityRule(asOf As Date) As OperationResult(Of GratuityRule) Implements IStatutoryRateService.GetGratuityRule
        Dim rule = _repository.GetGratuityRule(asOf)
        Return Check(rule, rule?.Verified, "gratuity rule", asOf, rule?.EffectiveFrom)
    End Function

    Public Function GetBonusRule(asOf As Date) As OperationResult(Of BonusRule) Implements IStatutoryRateService.GetBonusRule
        Dim rule = _repository.GetBonusRule(asOf)
        Return Check(rule, rule?.Verified, "bonus rule", asOf, rule?.EffectiveFrom)
    End Function

    Public Function GetTdsDeductionLimits(asOf As Date, regime As TdsRegime) As OperationResult(Of IReadOnlyList(Of TdsDeductionLimit)) _
        Implements IStatutoryRateService.GetTdsDeductionLimits
        Dim year = FinancialYearCalendar.StartYear(asOf)
        Dim label = $"TDS deduction limits for {If(regime = TdsRegime.Old, "the old", "the new")} regime, financial year {FinancialYearCalendar.Label(year)}"
        Dim rows = _repository.GetTdsDeductionLimits(year, regime)
        If rows Is Nothing OrElse rows.Count = 0 Then
            Return OperationResult(Of IReadOnlyList(Of TdsDeductionLimit)).Fail($"No {label} are set. Ask the CA to add them.")
        End If
        If _repository.IsVerificationRequired() Then
            Dim unverified = rows.Where(Function(row) Not row.Verified).Count()
            If unverified > 0 Then
                Return OperationResult(Of IReadOnlyList(Of TdsDeductionLimit)).Fail(
                    $"{unverified.ToString(CultureInfo.InvariantCulture)} of the {rows.Count.ToString(CultureInfo.InvariantCulture)} {label} have not been verified by the CA.")
            End If
        End If
        Return OperationResult(Of IReadOnlyList(Of TdsDeductionLimit)).Ok(rows)
    End Function

    Public Function GetHraMetroList(asOf As Date) As OperationResult(Of HraMetroList) Implements IStatutoryRateService.GetHraMetroList
        Dim rows = _repository.GetHraCities(asOf)
        If rows Is Nothing OrElse rows.Count = 0 Then
            Return OperationResult(Of HraMetroList).Fail($"No HRA city list is set for {Format(asOf)}. Ask the CA to add one.")
        End If
        Dim startDate = Format(rows.Min(Function(row) row.EffectiveFrom))
        If Not rows.Any(Function(row) row.IsOtherCities) Then
            Return OperationResult(Of HraMetroList).Fail($"The HRA city list from {startDate} has no row for other cities (city name *). Ask the CA to complete it.")
        End If
        If _repository.IsVerificationRequired() AndAlso rows.Any(Function(row) Not row.Verified) Then
            Return OperationResult(Of HraMetroList).Fail($"The HRA city list from {startDate} has not been fully verified by the CA.")
        End If
        Return OperationResult(Of HraMetroList).Ok(New HraMetroList(rows))
    End Function

    Public Function GetTdsRule(asOf As Date, regime As TdsRegime, ageBand As TdsAgeBand) As OperationResult(Of TdsRule) Implements IStatutoryRateService.GetTdsRule
        Dim year = FinancialYearCalendar.StartYear(asOf)
        Dim label = $"TDS rule for {If(regime = TdsRegime.Old, "the old", "the new")} regime, financial year {FinancialYearCalendar.Label(year)}, age band {AgeBandName(ageBand)}"
        Dim rule = _repository.GetTdsRule(year, regime, ageBand)
        If rule Is Nothing Then
            Return OperationResult(Of TdsRule).Fail($"No {label} is set. Ask the CA to add one.")
        End If
        If rule.Slabs.Count = 0 Then
            Return OperationResult(Of TdsRule).Fail($"The {label} has no tax slabs. Ask the CA to complete it.")
        End If
        If _repository.IsVerificationRequired() AndAlso Not rule.Verified Then
            Return OperationResult(Of TdsRule).Fail($"The {label} has not been verified by the CA.")
        End If
        Return OperationResult(Of TdsRule).Ok(rule)
    End Function

    Private Shared Function DueMonthsNeeded(frequency As PtFrequency) As Integer
        Select Case frequency
            Case PtFrequency.HalfYearly
                Return 2
            Case PtFrequency.Yearly
                Return 1
            Case Else
                Return 0
        End Select
    End Function

    Private Shared Function AgeBandName(ageBand As TdsAgeBand) As String
        Select Case ageBand
            Case TdsAgeBand.General
                Return "below 60"
            Case TdsAgeBand.Senior
                Return "60 to 79"
            Case Else
                Return "80 and above"
        End Select
    End Function

    Public Function GetLabourWelfareRule(stateId As Integer, asOf As Date) As OperationResult(Of LabourWelfareRule) Implements IStatutoryRateService.GetLabourWelfareRule
        Dim rule = _repository.GetLabourWelfareRule(stateId, asOf)
        Return Check(rule, rule?.Verified, "labour welfare rule", asOf, rule?.EffectiveFrom)
    End Function

    Public Function GetMinimumWages(stateId As Integer, asOf As Date) As OperationResult(Of IReadOnlyList(Of MinimumWageRate)) Implements IStatutoryRateService.GetMinimumWages
        Dim rows = _repository.GetMinimumWages(stateId, asOf)
        If rows Is Nothing OrElse rows.Count = 0 Then
            Return OperationResult(Of IReadOnlyList(Of MinimumWageRate)).Fail($"No minimum wage is set for {Format(asOf)}. Ask the CA to add one.")
        End If
        If _repository.IsVerificationRequired() AndAlso rows.Any(Function(row) Not row.Verified) Then
            Return OperationResult(Of IReadOnlyList(Of MinimumWageRate)).Fail($"The minimum wage from {Format(rows.Min(Function(row) row.EffectiveFrom))} has not been fully verified by the CA.")
        End If
        Return OperationResult(Of IReadOnlyList(Of MinimumWageRate)).Ok(rows)
    End Function

    Public Function GetTaxFormNo(purposeKey As String, financialYear As Integer) As OperationResult(Of String) Implements IStatutoryRateService.GetTaxFormNo
        RequireKey(purposeKey)
        Return CheckReference(_repository.GetTaxForm(purposeKey, financialYear), $"form number for {purposeKey}", financialYear)
    End Function

    Public Function GetTaxSectionRef(topicKey As String, financialYear As Integer) As OperationResult(Of String) Implements IStatutoryRateService.GetTaxSectionRef
        RequireKey(topicKey)
        Return CheckReference(_repository.GetTaxSectionRef(topicKey, financialYear), $"section reference for {topicKey}", financialYear)
    End Function

    Private Shared Sub RequireKey(key As String)
        If String.IsNullOrWhiteSpace(key) Then
            Throw New ArgumentException("A purpose or topic key is needed.", NameOf(key))
        End If
    End Sub

    Private Function CheckReference(row As TaxReference, label As String, financialYear As Integer) As OperationResult(Of String)
        Dim year = FinancialYearCalendar.Label(financialYear)
        If row Is Nothing OrElse String.IsNullOrWhiteSpace(row.Value) Then
            Return OperationResult(Of String).Fail($"No {label} is set for financial year {year}. Ask the CA to add one.")
        End If
        If _repository.IsVerificationRequired() AndAlso Not row.Verified Then
            Return OperationResult(Of String).Fail($"The {label} for financial year {year} has not been verified by the CA.")
        End If
        Return OperationResult(Of String).Ok(row.Value)
    End Function

    Private Function Check(Of T As Class)(row As T, verified As Boolean?, label As String, asOf As Date, effectiveFrom As Date?) As OperationResult(Of T)
        If row Is Nothing Then
            Return OperationResult(Of T).Fail($"No {label} is set for {Format(asOf)}. Ask the CA to add one.")
        End If
        If _repository.IsVerificationRequired() AndAlso Not verified.GetValueOrDefault() Then
            Return OperationResult(Of T).Fail($"The {label} from {Format(effectiveFrom.GetValueOrDefault())} has not been verified by the CA.")
        End If
        Return OperationResult(Of T).Ok(row)
    End Function

    Private Shared Function Format(value As Date) As String
        Return value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)
    End Function
End Class
