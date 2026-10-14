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
