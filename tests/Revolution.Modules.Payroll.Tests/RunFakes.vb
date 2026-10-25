Imports Revolution.Core
Imports Revolution.Modules.Payroll

Friend NotInheritable Class RunTestData
    Private Sub New()
    End Sub

    Friend Shared Function Salary() As IReadOnlyList(Of SalaryLineView)
        Return New List(Of SalaryLineView) From {
            New SalaryLineView(1, "BASIC", "Basic", True, False, True, 10000D),
            New SalaryLineView(2, "DA", "Dearness allowance", False, True, True, 2000D),
            New SalaryLineView(3, "HRA", "House rent allowance", False, False, False, 4000D)}
    End Function

    Friend Shared Function Facts(Optional employeeId As Integer = 101, Optional code As String = "E101", Optional epfMember As Boolean = True,
                                 Optional esiMember As Boolean = True, Optional ptStateId As Integer? = 1, Optional paidDays As Decimal = 31D,
                                 Optional overtimeHours As Decimal = 0D, Optional hoursPerDay As Decimal? = 8D, Optional unmarked As Integer = 0,
                                 Optional carryIn As Decimal = 0D, Optional withSalary As Boolean = True,
                                 Optional installments As IReadOnlyList(Of OpenInstallment) = Nothing) As RunEmployeeFacts
        Return New RunEmployeeFacts(employeeId, 1, code, False, 1, ptStateId, epfMember, False, False, esiMember, False, paidDays, overtimeHours,
                                    hoursPerDay, unmarked, carryIn, 0D, If(withSalary, Salary(), New List(Of SalaryLineView)()), installments)
    End Function

    Friend Shared Function Rates() As RunRates
        Dim pt As New Dictionary(Of Integer, PtRule) From {{1, StatutoryTestRates.Pt(PtFrequency.Monthly, Nothing)}}
        Return New RunRates(StatutoryTestRates.Epf(), StatutoryTestRates.Esi(), StatutoryTestRates.CodeWage(), pt)
    End Function

    Friend Shared Function Policy() As PolicySettings
        Return New PolicySettings(1, 2D, False, False, 26D, 25D)
    End Function

    Friend Shared Function October() As RunPeriod
        Return New RunPeriod(New Date(2026, 10, 1), New Date(2026, 10, 31))
    End Function

    Friend Shared Function Line(employeeId As Integer, Optional loanEmi As Decimal = 0D) As RunLineResult
        Return New RunLineResult(employeeId, 1, 31D, 0D, 0D, 16000D, 12000D, 12000D, 1440D, 440D, 1000D, 60D, 60D, 90D, 390D, 200D, loanEmi, 0D, 0D, 0D, 0D,
                                 1730D + loanEmi, 14270D - loanEmi, New List(Of EarningAmount) From {New EarningAmount(1, 10000D)})
    End Function
End Class

Friend NotInheritable Class FakeRunRepository
    Implements IPayrollRunRepository

    Public ReadOnly Runs As New Dictionary(Of Integer, PayrollRunHeader)()
    Public ReadOnly Lines As New Dictionary(Of Integer, List(Of RunLineResult))()
    Public ReadOnly Inputs As New List(Of RunEmployeeFacts)()
    Public ReadOnly Installments As New List(Of EmployeeInstallment)()
    Public ReadOnly Changes As New List(Of String)()
    Public ReadOnly Recoveries As New List(Of RecoveryAllocation)()
    Public Created As RunPeriod
    Public RecoveriesReversed As Boolean
    Public FailStatus As PayrollRunStatus?
    Public FailStatusMessage As String
    Public FailStatusNumber As Integer
    Public FailRecoveryMessage As String
    Public FailRecoveryNumber As Integer
    Public FailCreate As String
    Private _nextId As Integer = 1

    Public Function AddRun(status As PayrollRunStatus, Optional employeeCount As Integer = 2) As Integer
        Dim id As Integer = _nextId
        _nextId += 1
        Runs(id) = New PayrollRunHeader(id, New Date(2026, 9, 26), New Date(2026, 10, 25), 1, status, employeeCount, 0D, 0D, 0D)
        Return id
    End Function

    Public Function StatusOf(runId As Integer) As PayrollRunStatus
        Return Runs(runId).Status
    End Function

    Public Function CreateRun(period As RunPeriod, runNo As Integer, userId As Integer?) As Integer Implements IPayrollRunRepository.CreateRun
        If FailCreate IsNot Nothing Then Throw New PayrollRuleException(FailCreate, 50314, Nothing)
        Created = period
        Dim id As Integer = _nextId
        _nextId += 1
        Runs(id) = New PayrollRunHeader(id, period.PeriodFrom, period.PeriodTo, runNo, PayrollRunStatus.Draft, 0, 0D, 0D, 0D)
        Return id
    End Function

    Public Function GetRun(runId As Integer) As PayrollRunHeader Implements IPayrollRunRepository.GetRun
        Dim header As PayrollRunHeader = Nothing
        Runs.TryGetValue(runId, header)
        Return header
    End Function

    Public Function ListLines(runId As Integer) As IReadOnlyList(Of RunLineResult) Implements IPayrollRunRepository.ListLines
        Dim list As List(Of RunLineResult) = Nothing
        If Lines.TryGetValue(runId, list) Then Return list
        Return New List(Of RunLineResult)()
    End Function

    Public Function ListEvents(runId As Integer) As IReadOnlyList(Of PayrollRunEventItem) Implements IPayrollRunRepository.ListEvents
        Return New List(Of PayrollRunEventItem)()
    End Function

    Public Function LoadInputs(period As RunPeriod) As IReadOnlyList(Of RunEmployeeFacts) Implements IPayrollRunRepository.LoadInputs
        Return Inputs
    End Function

    Public Sub ReplaceLines(runId As Integer, lines As IReadOnlyList(Of RunLineResult), userId As Integer?) Implements IPayrollRunRepository.ReplaceLines
        Me.Lines(runId) = lines.ToList()
        Dim h As PayrollRunHeader = Runs(runId)
        Runs(runId) = New PayrollRunHeader(h.RunId, h.PeriodFrom, h.PeriodTo, h.RunNo, PayrollRunStatus.Processed, lines.Count, lines.Sum(Function(l) l.GrossEarnings),
                                           lines.Sum(Function(l) l.TotalDeductions), lines.Sum(Function(l) l.NetPay))
    End Sub

    Public Sub ChangeStatus(runId As Integer, toStatus As PayrollRunStatus, action As String, reason As String) Implements IPayrollRunRepository.ChangeStatus
        If FailStatus.HasValue AndAlso FailStatus.Value = toStatus Then Throw New PayrollRuleException(FailStatusMessage, FailStatusNumber, Nothing)
        Dim h As PayrollRunHeader = Runs(runId)
        If Not RunStateMachine.CanChange(h.Status, toStatus) Then Throw New PayrollRuleException("This status change is not allowed.", 50313, Nothing)
        Runs(runId) = New PayrollRunHeader(h.RunId, h.PeriodFrom, h.PeriodTo, h.RunNo, toStatus, h.EmployeeCount, h.TotalGross, h.TotalDeductions, h.TotalNet)
        Changes.Add($"{action}:{toStatus}:{reason}")
    End Sub

    Public Function ListOpenInstallments(uptoMonthKey As Integer) As IReadOnlyList(Of EmployeeInstallment) Implements IPayrollRunRepository.ListOpenInstallments
        Return Installments
    End Function

    Public Sub SaveRecoveries(runId As Integer, recoveries As IReadOnlyList(Of RecoveryAllocation)) Implements IPayrollRunRepository.SaveRecoveries
        If FailRecoveryMessage IsNot Nothing Then Throw New PayrollRuleException(FailRecoveryMessage, FailRecoveryNumber, Nothing)
        Me.Recoveries.AddRange(recoveries)
    End Sub

    Public Sub ReverseRecoveries(runId As Integer) Implements IPayrollRunRepository.ReverseRecoveries
        RecoveriesReversed = True
    End Sub
End Class

Friend NotInheritable Class FakeRates
    Implements IStatutoryRateService

    Public EpfFailure As String

    Public Function GetEpfRate(asOf As Date) As OperationResult(Of EpfRate) Implements IStatutoryRateService.GetEpfRate
        If EpfFailure IsNot Nothing Then Return OperationResult(Of EpfRate).Fail(EpfFailure)
        Return OperationResult(Of EpfRate).Ok(StatutoryTestRates.Epf())
    End Function

    Public Function GetEsiRate(asOf As Date) As OperationResult(Of EsiRate) Implements IStatutoryRateService.GetEsiRate
        Return OperationResult(Of EsiRate).Ok(StatutoryTestRates.Esi())
    End Function

    Public Function GetPtRule(stateId As Integer, asOf As Date) As OperationResult(Of PtRule) Implements IStatutoryRateService.GetPtRule
        Return OperationResult(Of PtRule).Ok(StatutoryTestRates.Pt(PtFrequency.Monthly, Nothing))
    End Function

    Public Function GetWageRule(asOf As Date) As OperationResult(Of WageRule) Implements IStatutoryRateService.GetWageRule
        Return OperationResult(Of WageRule).Ok(StatutoryTestRates.CodeWage())
    End Function

    Public Function GetGratuityRule(asOf As Date) As OperationResult(Of GratuityRule) Implements IStatutoryRateService.GetGratuityRule
        Return OperationResult(Of GratuityRule).Fail("Not used in these tests.")
    End Function

    Public Function GetBonusRule(asOf As Date) As OperationResult(Of BonusRule) Implements IStatutoryRateService.GetBonusRule
        Return OperationResult(Of BonusRule).Fail("Not used in these tests.")
    End Function

    Public Function GetTdsRule(asOf As Date, regime As TdsRegime, ageBand As TdsAgeBand) As OperationResult(Of TdsRule) Implements IStatutoryRateService.GetTdsRule
        Return OperationResult(Of TdsRule).Fail("Not used in these tests.")
    End Function

    Public Function GetTdsDeductionLimits(asOf As Date, regime As TdsRegime) As OperationResult(Of IReadOnlyList(Of TdsDeductionLimit)) Implements IStatutoryRateService.GetTdsDeductionLimits
        Return OperationResult(Of IReadOnlyList(Of TdsDeductionLimit)).Fail("Not used in these tests.")
    End Function

    Public Function GetHraMetroList(asOf As Date) As OperationResult(Of HraMetroList) Implements IStatutoryRateService.GetHraMetroList
        Return OperationResult(Of HraMetroList).Fail("Not used in these tests.")
    End Function

    Public Function GetLabourWelfareRule(stateId As Integer, asOf As Date) As OperationResult(Of LabourWelfareRule) Implements IStatutoryRateService.GetLabourWelfareRule
        Return OperationResult(Of LabourWelfareRule).Fail("Not used in these tests.")
    End Function

    Public Function GetMinimumWages(stateId As Integer, asOf As Date) As OperationResult(Of IReadOnlyList(Of MinimumWageRate)) Implements IStatutoryRateService.GetMinimumWages
        Return OperationResult(Of IReadOnlyList(Of MinimumWageRate)).Fail("Not used in these tests.")
    End Function
End Class

Friend NotInheritable Class FakeApprovals
    Implements IApprovalService

    Public Required As Boolean
    Public Cleared As Boolean
    Public SubmitFailure As String
    Public LastEntityName As String
    Public LastEntityId As String
    Public LastSummary As String

    Public Function IsRequired(moduleKey As ModuleKey) As Boolean Implements IApprovalService.IsRequired
        Return Required
    End Function

    Public Function Submit(moduleKey As ModuleKey, entityName As String, entityId As String, summary As String) As OperationResult(Of Integer) Implements IApprovalService.Submit
        If SubmitFailure IsNot Nothing Then Return OperationResult(Of Integer).Fail(SubmitFailure)
        LastEntityName = entityName
        LastEntityId = entityId
        LastSummary = summary
        Return OperationResult(Of Integer).Ok(1)
    End Function

    Public Function Approve(approvalId As Integer, Optional comment As String = "") As OperationResult Implements IApprovalService.Approve
        Return OperationResult.Ok()
    End Function

    Public Function Reject(approvalId As Integer, reason As String) As OperationResult Implements IApprovalService.Reject
        Return OperationResult.Ok()
    End Function

    Public Function Cancel(approvalId As Integer) As OperationResult Implements IApprovalService.Cancel
        Return OperationResult.Ok()
    End Function

    Public Function IsCleared(moduleKey As ModuleKey, entityName As String, entityId As String) As Boolean Implements IApprovalService.IsCleared
        Return Not Required OrElse Cleared
    End Function

    Public Function GetLatest(moduleKey As ModuleKey, entityName As String, entityId As String) As ApprovalRequest Implements IApprovalService.GetLatest
        Return Nothing
    End Function

    Public Function ListPending(moduleKey As ModuleKey) As IReadOnlyList(Of ApprovalRequest) Implements IApprovalService.ListPending
        Return New List(Of ApprovalRequest)()
    End Function
End Class
