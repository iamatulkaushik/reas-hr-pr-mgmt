Imports System.Globalization
Imports System.Text.RegularExpressions
Imports Revolution.Core

''' <summary>
''' Expense categories and claims for the signed-in company (Phase 9 step 0602, Platinum plan, PlanFeature.AssetsAndExpenses). Owner decisions, 2026-11-11: the Operator enters a claim, only the Owner approves it,
''' it is paid separately in cash or by bank, and the Associate has no access. Permissions on Expenses: View to look, Create to enter a claim or add a category, Edit to change a category or cancel a claim,
''' Approve to approve, reject and record payment. The audit text holds no amount and no bill number.
''' </summary>
Public NotInheritable Class ExpenseService
    Public Const MaxAmount As Decimal = 9999999.99D
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const NotInPlan As String = "Expenses are not part of your plan."
    Private Const MinReasonLength As Integer = 3
    Private Const MaxReasonLength As Integer = 200

    Private Shared ReadOnly BillPattern As New Regex("^[A-Za-z0-9._/ -]{1,40}$", RegexOptions.Compiled Or RegexOptions.CultureInvariant)

    Private ReadOnly _repository As IExpenseRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _today As Func(Of Date)

    Public Sub New(repository As IExpenseRepository, permissions As IPermissionService, entitlements As IEntitlementService, audit As IAuditService,
                   session As ISessionContext, today As Func(Of Date))
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        If today Is Nothing Then Throw New ArgumentNullException(NameOf(today))
        _repository = repository
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
        _session = session
        _today = today
    End Sub

    Public Function ListCategories(includeInactive As Boolean) As OperationResult(Of IReadOnlyList(Of ExpenseCategoryItem))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of ExpenseCategoryItem)).Fail(allowed.Message)
        Return OperationResult(Of IReadOnlyList(Of ExpenseCategoryItem)).Ok(_repository.ListCategories(includeInactive))
    End Function

    ''' <summary>Adds a category (CategoryId Nothing, needs Create) or changes one (needs Edit). A limit of Nothing means no limit; the monthly limit cannot be below the per-claim limit. Returns its id.</summary>
    Public Function SaveCategory(category As ExpenseCategoryWrite) As OperationResult(Of Integer)
        If category Is Nothing Then Return OperationResult(Of Integer).Fail("Give the category.")
        Dim allowed As OperationResult = CheckAccess(If(category.CategoryId.HasValue, PermissionAction.Edit, PermissionAction.Create))
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        Dim name As String = If(category.Name, String.Empty).Trim()
        If name.Length = 0 OrElse name.Length > 60 Then Return OperationResult(Of Integer).Fail("Give the category a name of 1 to 60 characters.")
        Dim perClaim As Decimal? = Rounded(category.PerClaimLimit)
        Dim perMonth As Decimal? = Rounded(category.PerMonthLimit)
        If (perClaim.HasValue AndAlso (perClaim.Value <= 0D OrElse perClaim.Value > MaxAmount)) OrElse (perMonth.HasValue AndAlso (perMonth.Value <= 0D OrElse perMonth.Value > MaxAmount)) Then
            Return OperationResult(Of Integer).Fail("A limit must be more than 0 rupees, or left empty for no limit.")
        End If
        If perClaim.HasValue AndAlso perMonth.HasValue AndAlso perMonth.Value < perClaim.Value Then
            Return OperationResult(Of Integer).Fail("The monthly limit cannot be below the limit per claim.")
        End If
        Dim clean As New ExpenseCategoryWrite With {.CategoryId = category.CategoryId, .Name = name, .PerClaimLimit = perClaim, .PerMonthLimit = perMonth, .IsActive = category.IsActive}
        Dim id As Integer
        Try
            id = _repository.SaveCategory(clean, CurrentUserId())
        Catch ex As ExpensesRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = Log(If(category.CategoryId.HasValue, "ExpenseCategory.Change", "ExpenseCategory.Add"), "ExpenseCategory", id, "Expense category saved.")
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    Public Function ListClaims(employeeId As Integer?, status As ExpenseClaimStatus?, fromDate As Date?, toDate As Date?) As OperationResult(Of IReadOnlyList(Of ExpenseClaimItem))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of ExpenseClaimItem)).Fail(allowed.Message)
        If fromDate.HasValue AndAlso toDate.HasValue AndAlso fromDate.Value.Date > toDate.Value.Date Then
            Return OperationResult(Of IReadOnlyList(Of ExpenseClaimItem)).Fail("The from date is after the to date.")
        End If
        Return OperationResult(Of IReadOnlyList(Of ExpenseClaimItem)).Ok(_repository.ListClaims(employeeId, status, fromDate, toDate))
    End Function

    ''' <summary>
    ''' Enters a claim for an employee, Submitted. The category's limits (per claim, and per employee per month) and the bill (vendor and number, once) are checked by the database. Returns the claim id.
    ''' </summary>
    Public Function AddClaim(claim As ExpenseClaimWrite) As OperationResult(Of Integer)
        If claim Is Nothing Then Return OperationResult(Of Integer).Fail("Give the claim.")
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Create)
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        Dim problem As String = ValidateClaim(claim)
        If problem.Length > 0 Then Return OperationResult(Of Integer).Fail(problem)
        Dim clean As New ExpenseClaimWrite With {.EmployeeId = claim.EmployeeId, .CategoryId = claim.CategoryId, .ExpenseDate = claim.ExpenseDate.Date,
                                                 .Amount = Math.Round(claim.Amount, 2, MidpointRounding.AwayFromZero), .BillNo = If(claim.BillNo, String.Empty).Trim(),
                                                 .BillDate = claim.BillDate, .Vendor = If(claim.Vendor, String.Empty).Trim(), .Note = If(claim.Note, String.Empty).Trim()}
        Dim id As Integer
        Try
            id = _repository.AddClaim(clean, CurrentUserId())
        Catch ex As ExpensesRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = Log("ExpenseClaim.Add", "ExpenseClaim", id, "Expense claim entered.")
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    ''' <summary>The Owner approves a Submitted claim, or rejects it with a reason. Needs Approve.</summary>
    Public Function Decide(claimId As Integer, approve As Boolean, reason As String) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Approve)
        If Not allowed.Succeeded Then Return allowed
        If claimId <= 0 Then Return OperationResult.Fail("Pick a claim.")
        Dim cleanReason As String = If(reason, String.Empty).Trim()
        If cleanReason.Length > MaxReasonLength Then Return OperationResult.Fail($"The reason can have at most {MaxReasonLength} characters.")
        If Not approve AndAlso cleanReason.Length < MinReasonLength Then Return OperationResult.Fail("Give a reason for rejecting a claim.")
        Try
            _repository.Decide(claimId, approve, cleanReason, CurrentUserId())
        Catch ex As ExpensesRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return Log(If(approve, "ExpenseClaim.Approve", "ExpenseClaim.Reject"), "ExpenseClaim", claimId, If(approve, "Expense claim approved.", "Expense claim rejected."))
    End Function

    ''' <summary>Records that an Approved claim was paid, in cash or by bank, outside payroll. Paying by bank needs a reference (for example the transfer number). Needs Approve.</summary>
    Public Function Pay(claimId As Integer, paidDate As Date, mode As ExpensePaymentMode, reference As String) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Approve)
        If Not allowed.Succeeded Then Return allowed
        If claimId <= 0 Then Return OperationResult.Fail("Pick a claim.")
        If mode <> ExpensePaymentMode.Cash AndAlso mode <> ExpensePaymentMode.Bank Then Return OperationResult.Fail("Pay in cash or by bank.")
        If paidDate.Date > _today().Date Then Return OperationResult.Fail("The payment date cannot be in the future.")
        Dim cleanRef As String = If(reference, String.Empty).Trim()
        If cleanRef.Length > 60 Then Return OperationResult.Fail("The payment reference can have at most 60 characters.")
        If mode = ExpensePaymentMode.Bank AndAlso cleanRef.Length < MinReasonLength Then Return OperationResult.Fail("Give the bank reference of the payment.")
        Try
            _repository.Pay(claimId, paidDate.Date, mode, cleanRef, CurrentUserId())
        Catch ex As ExpensesRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return Log("ExpenseClaim.Pay", "ExpenseClaim", claimId, "Expense claim paid.")
    End Function

    ''' <summary>Cancels a Submitted or Approved claim, with a reason. A Paid claim cannot be cancelled. Needs Edit (the Owner).</summary>
    Public Function Cancel(claimId As Integer, reason As String) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return allowed
        If claimId <= 0 Then Return OperationResult.Fail("Pick a claim.")
        Dim cleanReason As String = If(reason, String.Empty).Trim()
        If cleanReason.Length < MinReasonLength OrElse cleanReason.Length > MaxReasonLength Then Return OperationResult.Fail("Give a reason of 3 to 200 characters.")
        Try
            _repository.Cancel(claimId, cleanReason, CurrentUserId())
        Catch ex As ExpensesRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return Log("ExpenseClaim.Cancel", "ExpenseClaim", claimId, "Expense claim cancelled.")
    End Function

    Private Function ValidateClaim(claim As ExpenseClaimWrite) As String
        If claim.EmployeeId <= 0 Then Return "Pick an employee."
        If claim.CategoryId <= 0 Then Return "Pick a category."
        If claim.ExpenseDate.Date > _today().Date Then Return "The expense date cannot be in the future."
        If claim.Amount <= 0D OrElse claim.Amount > MaxAmount Then Return "The amount must be more than 0 rupees."
        Dim bill As String = If(claim.BillNo, String.Empty).Trim()
        Dim vendor As String = If(claim.Vendor, String.Empty).Trim()
        If bill.Length > 0 AndAlso Not BillPattern.IsMatch(bill) Then Return "The bill number is 1 to 40 letters, digits or . _ / - and spaces."
        If bill.Length > 0 AndAlso vendor.Length = 0 Then Return "Give the vendor of the bill."
        If vendor.Length > 80 Then Return "The vendor can have at most 80 characters."
        If claim.BillDate.HasValue AndAlso claim.BillDate.Value.Date > _today().Date Then Return "The bill date cannot be in the future."
        If If(claim.Note, String.Empty).Trim().Length > 200 Then Return "The note can have at most 200 characters."
        Return String.Empty
    End Function

    Private Shared Function Rounded(value As Decimal?) As Decimal?
        Return If(value.HasValue, CType(Math.Round(value.Value, 2, MidpointRounding.AwayFromZero), Decimal?), CType(Nothing, Decimal?))
    End Function

    Private Function CheckAccess(action As PermissionAction) As OperationResult
        If Not _entitlements.IsEnabled(PlanFeature.AssetsAndExpenses) Then Return OperationResult.Fail(NotInPlan)
        If Not _permissions.IsAllowed(ModuleKey.Expenses, action) Then Return OperationResult.Fail(NotAllowed)
        Return OperationResult.Ok()
    End Function

    Private Function Log(action As String, entity As String, id As Integer, summary As String) As OperationResult
        Return _audit.Record(New AuditEntry(ModuleKey.Expenses, action, entity, id.ToString(CultureInfo.InvariantCulture), summary))
    End Function

    Private Function CurrentUserId() As Integer?
        Dim user As UserSession = _session.Current
        Return If(user Is Nothing, CType(Nothing, Integer?), user.UserId)
    End Function
End Class
