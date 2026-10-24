''' <summary>Reads the effective-dated stat.* rows (ADR 0004, ADR 0010). Each method returns the row in force on the date, or Nothing.</summary>
Public Interface IStatutoryRateRepository
    Function GetEpfRate(asOf As Date) As EpfRate
    Function GetEsiRate(asOf As Date) As EsiRate
    ''' <summary>The state's rule with its slabs, or Nothing when the state has no rule on that date.</summary>
    Function GetPtRule(stateId As Integer, asOf As Date) As PtRule
    Function GetWageRule(asOf As Date) As WageRule
    ''' <summary>The gratuity rule in force on the date, or Nothing when there is none.</summary>
    Function GetGratuityRule(asOf As Date) As GratuityRule
    ''' <summary>The bonus rule in force on the date, or Nothing when there is none.</summary>
    Function GetBonusRule(asOf As Date) As BonusRule
    ''' <summary>The income tax rule for a financial year (start year), regime and age band with its slabs and surcharge bands, or Nothing when there is no parameter row.</summary>
    Function GetTdsRule(financialYear As Integer, regime As TdsRegime, ageBand As TdsAgeBand) As TdsRule
    ''' <summary>The deduction limits for a financial year (start year) and regime, or an empty list when there are none.</summary>
    Function GetTdsDeductionLimits(financialYear As Integer, regime As TdsRegime) As IReadOnlyList(Of TdsDeductionLimit)
    ''' <summary>The HRA city rows in force on the date (all rows of the latest list on or before it), or an empty list.</summary>
    Function GetHraCities(asOf As Date) As IReadOnlyList(Of HraCity)
    ''' <summary>The cfg.Setting RateVerificationRequired. True when the setting is missing, so the safe answer is the default.</summary>
    Function IsVerificationRequired() As Boolean
End Interface
