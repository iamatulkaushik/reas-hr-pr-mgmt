Imports Revolution.Core
Imports Revolution.Modules.Payroll

Friend NotInheritable Class FakeSalaryRepository
    Implements ISalaryRepository

    Public Policy As New PolicySettings(1, 2D, False, False, 26D, 25D)
    Public ReadOnly Components As New List(Of SalaryComponentItem) From {
        New SalaryComponentItem(1, "BASIC", "Basic", True, False, True, 10, True),
        New SalaryComponentItem(2, "DA", "Dearness allowance", False, True, True, 20, True),
        New SalaryComponentItem(3, "HRA", "House rent allowance", False, False, False, 30, True),
        New SalaryComponentItem(4, "OLD", "Retired head", False, False, False, 40, False)}
    Public ReadOnly SavedPolicies As New List(Of PolicySettings)()
    Public ReadOnly SavedComponents As New List(Of SalaryComponentItem)()
    Public ReadOnly SavedSalaries As New List(Of String)()
    Public SalaryInForce As EmployeeSalaryView
    Public LastUserId As Integer?
    Public FailWith As String

    Public Function GetPolicy() As PolicySettings Implements ISalaryRepository.GetPolicy
        Return Policy
    End Function

    Public Sub SavePolicy(policy As PolicySettings, userId As Integer?) Implements ISalaryRepository.SavePolicy
        ThrowIfFailing()
        SavedPolicies.Add(policy)
        LastUserId = userId
    End Sub

    Public Function ListComponents() As IReadOnlyList(Of SalaryComponentItem) Implements ISalaryRepository.ListComponents
        Return Components
    End Function

    Public Function SaveComponent(component As SalaryComponentItem) As Integer Implements ISalaryRepository.SaveComponent
        ThrowIfFailing()
        SavedComponents.Add(component)
        Return If(component.ComponentId, 99)
    End Function

    Public Function GetSalaryAt(employeeId As Integer, asOf As Date) As EmployeeSalaryView Implements ISalaryRepository.GetSalaryAt
        Return SalaryInForce
    End Function

    Public Function SaveSalary(employeeId As Integer, effectiveFrom As Date, reason As String, lines As IReadOnlyList(Of SalaryLineWrite),
                               userId As Integer?) As Integer Implements ISalaryRepository.SaveSalary
        ThrowIfFailing()
        SavedSalaries.Add($"{employeeId}:{effectiveFrom:yyyy-MM-dd}:{reason}:{lines.Count}")
        LastUserId = userId
        Return 500
    End Function

    Private Sub ThrowIfFailing()
        If FailWith IsNot Nothing Then
            Throw New PayrollRuleException(FailWith)
        End If
    End Sub
End Class

Friend NotInheritable Class FakePayPermissions
    Implements IPermissionService

    Public ReadOnly Allowed As New HashSet(Of PermissionAction)()

    Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
        Return moduleKey = ModuleKey.Payroll AndAlso Allowed.Contains(action)
    End Function
End Class

Friend NotInheritable Class FakePayEntitlements
    Implements IEntitlementService

    Public PayrollEnabled As Boolean = True
    Public ReadOnly Disabled As New HashSet(Of PlanFeature)()

    Public ReadOnly Property CurrentPlan As PlanTier Implements IEntitlementService.CurrentPlan
        Get
            Return PlanTier.Gold
        End Get
    End Property

    Public Function IsEnabled(feature As PlanFeature) As Boolean Implements IEntitlementService.IsEnabled
        If Disabled.Contains(feature) Then Return False
        Return feature <> PlanFeature.BasicPayroll OrElse PayrollEnabled
    End Function
End Class

Friend NotInheritable Class FakePayAudit
    Implements IAuditService

    Public ReadOnly Entries As New List(Of AuditEntry)()
    Public Fail As Boolean

    Public Function Record(entry As AuditEntry) As OperationResult Implements IAuditService.Record
        If Fail Then Return OperationResult.Fail("Audit failed.")
        Entries.Add(entry)
        Return OperationResult.Ok()
    End Function
End Class

Friend NotInheritable Class FakePaySession
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
