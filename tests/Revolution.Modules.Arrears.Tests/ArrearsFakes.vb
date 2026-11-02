Imports Revolution.Core
Imports Revolution.Modules.Arrears
Imports Revolution.Modules.Payroll

Friend NotInheritable Class FakeArrearsRepository
    Implements IArrearsRepository

    Public ReadOnly Added As New List(Of NewArrearsClaim)()
    Public ReadOnly Cancelled As New List(Of Tuple(Of Integer, String, Integer?))()
    Public Rows As New List(Of ArrearsClaimRow)()
    Public ReportRows As New List(Of ArrearsReportRow)()
    Public EcrRows As New List(Of ArrearsEcrLine)()
    Public LastFilter As ArrearsFilter
    Public LastReport As Tuple(Of Date, Date)
    Public FailWith As String

    Public Function Add(claim As NewArrearsClaim) As Integer Implements IArrearsRepository.Add
        If FailWith IsNot Nothing Then Throw New PayrollRuleException(FailWith)
        Added.Add(claim)
        Return Added.Count
    End Function

    Public Function List(filter As ArrearsFilter) As IReadOnlyList(Of ArrearsClaimRow) Implements IArrearsRepository.List
        LastFilter = filter
        Return Rows
    End Function

    Public Function [Get](claimId As Integer) As ArrearsClaimDetail Implements IArrearsRepository.Get
        If FailWith IsNot Nothing Then Throw New PayrollRuleException(FailWith)
        Return New ArrearsClaimDetail(Rows.Single(Function(r As ArrearsClaimRow) r.ClaimId = claimId), New List(Of ArrearsMonthRecord)())
    End Function

    Public Sub Cancel(claimId As Integer, reason As String, userId As Integer?) Implements IArrearsRepository.Cancel
        If FailWith IsNot Nothing Then Throw New PayrollRuleException(FailWith)
        Cancelled.Add(Tuple.Create(claimId, reason, userId))
    End Sub

    Public Function EcrLines(wageMonth As Date) As IReadOnlyList(Of ArrearsEcrLine) Implements IArrearsRepository.EcrLines
        Return EcrRows
    End Function

    Public Function Report(fromMonth As Date, toMonth As Date) As IReadOnlyList(Of ArrearsReportRow) Implements IArrearsRepository.Report
        LastReport = Tuple.Create(fromMonth, toMonth)
        Return ReportRows
    End Function
End Class

Friend NotInheritable Class FakeArrearsPermissions
    Implements IPermissionService

    Public ReadOnly Allowed As New HashSet(Of PermissionAction)()

    Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
        Return (moduleKey = ModuleKey.Payroll OrElse moduleKey = ModuleKey.Statutory) AndAlso Allowed.Contains(action)
    End Function
End Class

Friend NotInheritable Class FakeArrearsEntitlements
    Implements IEntitlementService

    Public PayrollEnabled As Boolean = True

    Public ReadOnly Property CurrentPlan As PlanTier Implements IEntitlementService.CurrentPlan
        Get
            Return PlanTier.Gold
        End Get
    End Property

    Public Function IsEnabled(feature As PlanFeature) As Boolean Implements IEntitlementService.IsEnabled
        Return feature <> PlanFeature.BasicPayroll OrElse PayrollEnabled
    End Function
End Class

Friend NotInheritable Class FakeArrearsAudit
    Implements IAuditService

    Public ReadOnly Entries As New List(Of AuditEntry)()

    Public Function Record(entry As AuditEntry) As OperationResult Implements IAuditService.Record
        Entries.Add(entry)
        Return OperationResult.Ok()
    End Function
End Class

Friend NotInheritable Class FakeArrearsSession
    Implements ISessionContext

    Public ReadOnly Property Current As UserSession Implements ISessionContext.Current
        Get
            Return New UserSession(7, "associate1", UserRole.Associate, 1, PlanTier.Gold)
        End Get
    End Property

    Public ReadOnly Property IsAuthenticated As Boolean Implements ISessionContext.IsAuthenticated
        Get
            Return True
        End Get
    End Property

    Public Sub SignIn(session As UserSession) Implements ISessionContext.SignIn
    End Sub

    Public Sub SignOut() Implements ISessionContext.SignOut
    End Sub
End Class

''' <summary>Rate rows for tests only. Fixtures, not the rates the product uses.</summary>
Friend NotInheritable Class ArrearsTestRates
    Private Sub New()
    End Sub

    Public Shared Function Rates() As RunRates
        Dim epf As New EpfRate(New Date(2026, 4, 1), 12D, 3.67D, 8.33D, 0.5D, 0.5D, 0D, 15000D, 15000D, True, "test")
        Dim esi As New EsiRate(New Date(2026, 4, 1), 0.75D, 3.25D, 21000D, 0D, RateRounding.Up, True, "test")
        Dim rule As New WageRule(New Date(2026, 4, 1), False, 0D, "test", True, "test")
        Return New RunRates(epf, esi, rule, New Dictionary(Of Integer, PtRule)())
    End Function

    Public Shared Function Wage(basic As Decimal) As WageBreakdown
        Dim lines As New List(Of EarnedLine) From {New EarnedLine("BASIC", basic)}
        Return New WageBreakdown(lines, 0D, basic, basic, basic)
    End Function

    Public Shared Function Month(year As Integer, monthNo As Integer, paid As Decimal, due As Decimal) As ArrearsMonthInput
        Return New ArrearsMonthInput(New Date(year, monthNo, 1), Wage(paid), Wage(due), Rates(), 400D, False)
    End Function
End Class

Friend NotInheritable Class FakeArrearsSource
    Implements IArrearsSource

    Public ReadOnly Runs As New Dictionary(Of Date, PayrollRunHeader)()

    Public Function FindRegularRun(period As RunPeriod) As PayrollRunHeader Implements IArrearsSource.FindRegularRun
        Dim header As PayrollRunHeader = Nothing
        Runs.TryGetValue(period.PeriodFrom, header)
        Return header
    End Function
End Class

Friend NotInheritable Class FakeArrearsRates
    Implements IArrearsRates

    Public Function GetEpfRate(asOf As Date) As OperationResult(Of EpfRate) Implements IArrearsRates.GetEpfRate
        Return OperationResult(Of EpfRate).Ok(ArrearsTestRates.Rates().Epf)
    End Function

    Public Function GetEsiRate(asOf As Date) As OperationResult(Of EsiRate) Implements IArrearsRates.GetEsiRate
        Return OperationResult(Of EsiRate).Ok(ArrearsTestRates.Rates().Esi)
    End Function

    Public Function GetWageRule(asOf As Date) As OperationResult(Of WageRule) Implements IArrearsRates.GetWageRule
        Return OperationResult(Of WageRule).Ok(ArrearsTestRates.Rates().WageRule)
    End Function

    Public Function GetPtRule(stateId As Integer, asOf As Date) As OperationResult(Of PtRule) Implements IArrearsRates.GetPtRule
        Return OperationResult(Of PtRule).Fail("No professional tax rule in this fixture.")
    End Function
End Class

Friend NotInheritable Class FakeArrearsSalary
    Implements ISalaryRepository

    Public Function GetPolicy() As PolicySettings Implements ISalaryRepository.GetPolicy
        Return New PolicySettings(1, 2D, False, False, 26D, 25D)
    End Function

    Public Sub SavePolicy(policy As PolicySettings, userId As Integer?) Implements ISalaryRepository.SavePolicy
        Throw New NotSupportedException()
    End Sub

    Public Function ListComponents() As IReadOnlyList(Of SalaryComponentItem) Implements ISalaryRepository.ListComponents
        Throw New NotSupportedException()
    End Function

    Public Function SaveComponent(component As SalaryComponentItem) As Integer Implements ISalaryRepository.SaveComponent
        Throw New NotSupportedException()
    End Function

    Public Function GetSalaryAt(employeeId As Integer, asOf As Date) As EmployeeSalaryView Implements ISalaryRepository.GetSalaryAt
        Throw New NotSupportedException()
    End Function

    Public Function SaveSalary(employeeId As Integer, effectiveFrom As Date, reason As String, lines As IReadOnlyList(Of SalaryLineWrite), userId As Integer?) As Integer Implements ISalaryRepository.SaveSalary
        Throw New NotSupportedException()
    End Function
End Class

Friend NotInheritable Class FakeArrearsRunRepository
    Implements IPayrollRunRepository

    Public ReadOnly Lines As New Dictionary(Of Integer, List(Of RunLineResult))()
    Public ReadOnly Facts As New Dictionary(Of Date, List(Of RunEmployeeFacts))()

    Public Function ListLines(runId As Integer) As IReadOnlyList(Of RunLineResult) Implements IPayrollRunRepository.ListLines
        Return If(Lines.ContainsKey(runId), Lines(runId), New List(Of RunLineResult)())
    End Function

    Public Function LoadInputs(period As RunPeriod) As IReadOnlyList(Of RunEmployeeFacts) Implements IPayrollRunRepository.LoadInputs
        Return If(Facts.ContainsKey(period.PeriodFrom), Facts(period.PeriodFrom), New List(Of RunEmployeeFacts)())
    End Function

    Public Function CreateRun(period As RunPeriod, runNo As Integer, userId As Integer?) As Integer Implements IPayrollRunRepository.CreateRun
        Throw New NotSupportedException()
    End Function

    Public Function GetRun(runId As Integer) As PayrollRunHeader Implements IPayrollRunRepository.GetRun
        Throw New NotSupportedException()
    End Function

    Public Function ListEvents(runId As Integer) As IReadOnlyList(Of PayrollRunEventItem) Implements IPayrollRunRepository.ListEvents
        Throw New NotSupportedException()
    End Function

    Public Sub ReplaceLines(runId As Integer, lines As IReadOnlyList(Of RunLineResult), userId As Integer?) Implements IPayrollRunRepository.ReplaceLines
        Throw New NotSupportedException()
    End Sub

    Public Sub ChangeStatus(runId As Integer, toStatus As PayrollRunStatus, action As String, reason As String) Implements IPayrollRunRepository.ChangeStatus
        Throw New NotSupportedException()
    End Sub

    Public Function ListOpenInstallments(uptoMonthKey As Integer) As IReadOnlyList(Of EmployeeInstallment) Implements IPayrollRunRepository.ListOpenInstallments
        Throw New NotSupportedException()
    End Function

    Public Sub SaveRecoveries(runId As Integer, recoveries As IReadOnlyList(Of RecoveryAllocation)) Implements IPayrollRunRepository.SaveRecoveries
        Throw New NotSupportedException()
    End Sub

    Public Sub ReverseRecoveries(runId As Integer) Implements IPayrollRunRepository.ReverseRecoveries
        Throw New NotSupportedException()
    End Sub
End Class

''' <summary>Run facts and stored lines for arrears tests: October 2026, one employee, basic only.</summary>
Friend NotInheritable Class ArrearsTestData
    Private Sub New()
    End Sub

    Public Shared Function October() As RunPeriod
        Return New RunPeriod(New Date(2026, 10, 1), New Date(2026, 10, 31))
    End Function

    Public Shared Function Policy() As PolicySettings
        Return New PolicySettings(1, 2D, False, False, 26D, 25D)
    End Function

    Public Shared Function Facts(basic As Decimal, Optional employeeId As Integer = 101, Optional epfMember As Boolean = True, Optional esiMember As Boolean = False) As RunEmployeeFacts
        Dim salary As New List(Of SalaryLineView) From {New SalaryLineView(1, "BASIC", "Basic", True, False, True, basic)}
        Return New RunEmployeeFacts(employeeId, 1, "E" & employeeId, False, 1, Nothing, epfMember, False, False, esiMember, False, 31D, 0D, 8D, 0, 0D, 0D, salary, Nothing)
    End Function

    ''' <summary>The line the regular run would have stored for these facts.</summary>
    Public Shared Function Paid(facts As RunEmployeeFacts) As RunLineResult
        Return PayrollRunCalculator.Calculate(facts, October(), Policy(), ArrearsTestRates.Rates()).Value
    End Function

    Public Shared Function Header(runId As Integer, status As PayrollRunStatus) As PayrollRunHeader
        Return New PayrollRunHeader(runId, New Date(2026, 10, 1), New Date(2026, 10, 31), 1, status, 1, 0D, 0D, 0D)
    End Function
End Class

Friend NotInheritable Class FakeArrearsReleaser
    Implements IExportIdReleaser

    Public ReadOnly Uans As New Dictionary(Of Integer, String)()
    Public Fail As String
    Public LastIds As IReadOnlyCollection(Of Integer)

    Public Function Release(kind As PayrollFileKind, employeeIds As IReadOnlyCollection(Of Integer)) As OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)) Implements IExportIdReleaser.Release
        LastIds = employeeIds
        If Fail IsNot Nothing Then Return OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)).Fail(Fail)
        Dim result As New Dictionary(Of Integer, ReleasedIds)()
        For Each id As Integer In employeeIds
            If Uans.ContainsKey(id) Then result(id) = New ReleasedIds With {.Uan = Uans(id)}
        Next
        Return OperationResult(Of IReadOnlyDictionary(Of Integer, ReleasedIds)).Ok(result)
    End Function
End Class
