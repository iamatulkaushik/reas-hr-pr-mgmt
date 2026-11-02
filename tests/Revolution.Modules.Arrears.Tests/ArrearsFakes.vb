Imports Revolution.Core
Imports Revolution.Modules.Arrears
Imports Revolution.Modules.Payroll

Friend NotInheritable Class FakeArrearsRepository
    Implements IArrearsRepository

    Public ReadOnly Added As New List(Of NewArrearsClaim)()
    Public ReadOnly Cancelled As New List(Of Tuple(Of Integer, String, Integer?))()
    Public Rows As New List(Of ArrearsClaimRow)()
    Public ReportRows As New List(Of ArrearsReportRow)()
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

    Public Function Report(fromMonth As Date, toMonth As Date) As IReadOnlyList(Of ArrearsReportRow) Implements IArrearsRepository.Report
        LastReport = Tuple.Create(fromMonth, toMonth)
        Return ReportRows
    End Function
End Class

Friend NotInheritable Class FakeArrearsPermissions
    Implements IPermissionService

    Public ReadOnly Allowed As New HashSet(Of PermissionAction)()

    Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
        Return moduleKey = ModuleKey.Payroll AndAlso Allowed.Contains(action)
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
