''' <summary>
''' Rates and rules for payroll. A missing row or (while verification is required) a row the CA has not signed off
''' returns a failure with a message the screen can show. Payroll must stop on a failure, never guess a rate.
''' </summary>
Public Interface IStatutoryRateService
    Function GetEpfRate(asOf As Date) As OperationResult(Of EpfRate)
    Function GetEsiRate(asOf As Date) As OperationResult(Of EsiRate)
    Function GetPtRule(stateId As Integer, asOf As Date) As OperationResult(Of PtRule)
    Function GetWageRule(asOf As Date) As OperationResult(Of WageRule)
    ''' <summary>The gratuity rule in force on the date. Fails when there is none or the CA has not signed it off; never returns a default.</summary>
    Function GetGratuityRule(asOf As Date) As OperationResult(Of GratuityRule)
    ''' <summary>The statutory bonus rule in force on the date. Fails when there is none or the CA has not signed it off; never returns a default.</summary>
    Function GetBonusRule(asOf As Date) As OperationResult(Of BonusRule)
    ''' <summary>The income tax rule for the financial year the date falls in. Fails when missing, without slabs, or not signed off.</summary>
    Function GetTdsRule(asOf As Date, regime As TdsRegime, ageBand As TdsAgeBand) As OperationResult(Of TdsRule)
    ''' <summary>
    ''' The deduction limits for the financial year the date falls in and the regime. Fails when there are none or, while verification is required,
    ''' when any row is not signed off. A section that is not in the list is not allowed (treat as zero).
    ''' </summary>
    Function GetTdsDeductionLimits(asOf As Date, regime As TdsRegime) As OperationResult(Of IReadOnlyList(Of TdsDeductionLimit))
    ''' <summary>
    ''' The HRA city list in force on the date. Fails when there is none, when it has no row for other cities ("*"), or, while verification is
    ''' required, when any row is not signed off. A city that is not listed gets the percentage of the "*" row.
    ''' </summary>
    Function GetHraMetroList(asOf As Date) As OperationResult(Of HraMetroList)
    ''' <summary>The labour welfare fund rule of the state in force on the date. Fails when there is none or the CA has not signed it off; never returns a default.</summary>
    Function GetLabourWelfareRule(stateId As Integer, asOf As Date) As OperationResult(Of LabourWelfareRule)
    ''' <summary>
    ''' The minimum wage rows of the state in force on the date (all zones and skills, per day and per month). Fails when there are none or, while
    ''' verification is required, when any row is not signed off. Payroll treats a wage below the minimum as a warning, not a stop.
    ''' </summary>
    Function GetMinimumWages(stateId As Integer, asOf As Date) As OperationResult(Of IReadOnlyList(Of MinimumWageRate))
End Interface
