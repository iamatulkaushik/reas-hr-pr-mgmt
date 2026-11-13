Imports Revolution.Core
Imports Revolution.Modules.Expenses

Friend NotInheritable Class FakeExpenseRepository
    Implements IExpenseRepository

    Public ReadOnly Categories As New List(Of ExpenseCategoryItem)()
    Public ReadOnly Claims As New List(Of ExpenseClaimItem)()
    Public ReadOnly SavedCategories As New List(Of ExpenseCategoryWrite)()
    Public ReadOnly AddedClaims As New List(Of ExpenseClaimWrite)()
    Public ReadOnly Decisions As New List(Of Tuple(Of Integer, Boolean, String))()
    Public ReadOnly Payments As New List(Of Tuple(Of Integer, Date, ExpensePaymentMode, String))()
    Public ReadOnly Cancelled As New List(Of Tuple(Of Integer, String))()
    Public FailWith As String

    Private Sub MaybeFail()
        If FailWith IsNot Nothing Then Throw New ExpensesRuleException(FailWith)
    End Sub

    Public Function ListCategories(includeInactive As Boolean) As IReadOnlyList(Of ExpenseCategoryItem) Implements IExpenseRepository.ListCategories
        Return Categories.Where(Function(c) includeInactive OrElse c.IsActive).ToList()
    End Function

    Public Function SaveCategory(category As ExpenseCategoryWrite, userId As Integer?) As Integer Implements IExpenseRepository.SaveCategory
        MaybeFail()
        SavedCategories.Add(category)
        Return If(category.CategoryId, 100 + SavedCategories.Count)
    End Function

    Public Function AddClaim(claim As ExpenseClaimWrite, userId As Integer?) As Integer Implements IExpenseRepository.AddClaim
        MaybeFail()
        AddedClaims.Add(claim)
        Return 200 + AddedClaims.Count
    End Function

    Public Function ListClaims(employeeId As Integer?, status As ExpenseClaimStatus?, fromDate As Date?, toDate As Date?) As IReadOnlyList(Of ExpenseClaimItem) Implements IExpenseRepository.ListClaims
        Return Claims.Where(Function(c) (Not employeeId.HasValue OrElse c.EmployeeId = employeeId) AndAlso (Not status.HasValue OrElse c.Status = status.Value)).ToList()
    End Function

    Public Sub Decide(claimId As Integer, approve As Boolean, reason As String, userId As Integer?) Implements IExpenseRepository.Decide
        MaybeFail()
        Decisions.Add(Tuple.Create(claimId, approve, reason))
    End Sub

    Public Sub Pay(claimId As Integer, paidDate As Date, mode As ExpensePaymentMode, reference As String, userId As Integer?) Implements IExpenseRepository.Pay
        MaybeFail()
        Payments.Add(Tuple.Create(claimId, paidDate, mode, reference))
    End Sub

    Public Sub Cancel(claimId As Integer, reason As String, userId As Integer?) Implements IExpenseRepository.Cancel
        MaybeFail()
        Cancelled.Add(Tuple.Create(claimId, reason))
    End Sub
End Class

Friend NotInheritable Class FakeExpensePermissions
    Implements IPermissionService

    Public ReadOnly Allowed As New HashSet(Of PermissionAction)()

    Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
        Return moduleKey = ModuleKey.Expenses AndAlso Allowed.Contains(action)
    End Function
End Class

Friend NotInheritable Class FakeExpenseEntitlements
    Implements IEntitlementService

    Public ExpensesEnabled As Boolean = True

    Public ReadOnly Property CurrentPlan As PlanTier Implements IEntitlementService.CurrentPlan
        Get
            Return PlanTier.Platinum
        End Get
    End Property

    Public Function IsEnabled(feature As PlanFeature) As Boolean Implements IEntitlementService.IsEnabled
        Return feature <> PlanFeature.AssetsAndExpenses OrElse ExpensesEnabled
    End Function
End Class

Friend NotInheritable Class FakeExpenseAudit
    Implements IAuditService

    Public ReadOnly Entries As New List(Of AuditEntry)()

    Public Function Record(entry As AuditEntry) As OperationResult Implements IAuditService.Record
        Entries.Add(entry)
        Return OperationResult.Ok()
    End Function
End Class

Friend NotInheritable Class FakeExpenseSession
    Implements ISessionContext

    Public ReadOnly Property Current As UserSession Implements ISessionContext.Current
        Get
            Return New UserSession(7, "operator1", UserRole.Operator, 1, PlanTier.Platinum)
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
