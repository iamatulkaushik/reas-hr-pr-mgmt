Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' Loan use cases: loan types, sanction with a computed schedule, cancel, list, schedule with recoveries. Needs the Loans plan feature and Payroll permissions:
''' View to read, Create for a new type or a sanction, Edit to change a type or cancel a loan. The loan copies the type's method and rate at sanction (a later change to
''' the type does not touch a running loan). The schedule is built here and checked by the database. EMI recovery is done by the payroll run, not here.
''' Audit text holds no amount.
''' </summary>
Public NotInheritable Class LoanService
    Private Const NotAllowed As String = "You are not allowed to do this."

    Private ReadOnly _repository As ILoanRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext

    Public Sub New(repository As ILoanRepository, permissions As IPermissionService, entitlements As IEntitlementService,
                   audit As IAuditService, session As ISessionContext)
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        _repository = repository
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
        _session = session
    End Sub

    Public Function ListTypes() As OperationResult(Of IReadOnlyList(Of LoanTypeItem))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of LoanTypeItem)).Fail(allowed.Message)
        Return OperationResult(Of IReadOnlyList(Of LoanTypeItem)).Ok(_repository.ListTypes())
    End Function

    ''' <summary>Adds a loan type (Create) or changes one (Edit). Returns the loan type id.</summary>
    Public Function SaveType(loanType As LoanTypeItem) As OperationResult(Of Integer)
        Dim isChange As Boolean = loanType IsNot Nothing AndAlso loanType.LoanTypeId.HasValue
        Dim allowed As OperationResult = CheckAccess(If(isChange, PermissionAction.Edit, PermissionAction.Create))
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        Dim check As OperationResult = LoanValidator.ValidateType(loanType)
        If Not check.Succeeded Then Return OperationResult(Of Integer).Fail(check.Message)
        Dim cleaned As New LoanTypeItem(loanType.LoanTypeId, loanType.Name.Trim(), loanType.Method, loanType.AnnualInterestPct, loanType.MaxInstallments, loanType.IsActive)
        Dim id As Integer
        Try
            id = _repository.SaveType(cleaned)
        Catch ex As PayrollRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Payroll, If(isChange, "LoanType.Edit", "LoanType.Add"), "LoanType",
                                                                      id.ToString(CultureInfo.InvariantCulture), $"Name {cleaned.Name}."))
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    ''' <summary>The schedule a sanction would create, without saving it. For the sanction screen.</summary>
    Public Function PreviewSchedule(loanTypeId As Integer, principal As Decimal, installments As Integer, firstDueMonthKey As Integer) As OperationResult(Of IReadOnlyList(Of LoanInstallmentPlan))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Create)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of LoanInstallmentPlan)).Fail(allowed.Message)
        Dim loanType As LoanTypeItem = FindType(loanTypeId)
        Dim check As OperationResult = LoanValidator.ValidateTerms(principal, installments, firstDueMonthKey, loanType)
        If Not check.Succeeded Then Return OperationResult(Of IReadOnlyList(Of LoanInstallmentPlan)).Fail(check.Message)
        Return OperationResult(Of IReadOnlyList(Of LoanInstallmentPlan)).Ok(LoanScheduleCalculator.Build(principal, loanType.AnnualInterestPct, installments, loanType.Method, firstDueMonthKey))
    End Function

    ''' <summary>Sanctions a loan with the type's method and rate and a computed schedule. Returns the loan id.</summary>
    Public Function Sanction(employeeId As Integer, loanTypeId As Integer, sanctionDate As Date, principal As Decimal, installments As Integer,
                             firstDueMonthKey As Integer) As OperationResult(Of Integer)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Create)
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        Dim loanType As LoanTypeItem = FindType(loanTypeId)
        Dim check As OperationResult = LoanValidator.ValidateSanction(employeeId, sanctionDate, principal, installments, firstDueMonthKey, loanType)
        If Not check.Succeeded Then Return OperationResult(Of Integer).Fail(check.Message)
        Dim schedule As IReadOnlyList(Of LoanInstallmentPlan) = LoanScheduleCalculator.Build(principal, loanType.AnnualInterestPct, installments, loanType.Method, firstDueMonthKey)
        Dim id As Integer
        Try
            id = _repository.CreateLoan(employeeId, loanTypeId, sanctionDate.Date, principal, loanType.Method, loanType.AnnualInterestPct, schedule, CurrentUserId())
        Catch ex As PayrollRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Payroll, "Loan.Sanction", "Loan", id.ToString(CultureInfo.InvariantCulture),
                                                                      $"Employee {employeeId.ToString(CultureInfo.InvariantCulture)}, {installments.ToString(CultureInfo.InvariantCulture)} installments from {firstDueMonthKey.ToString(CultureInfo.InvariantCulture)}."))
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    ''' <summary>Cancels a loan that nothing has been recovered on.</summary>
    Public Function Cancel(loanId As Integer) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return allowed
        If loanId <= 0 Then Return OperationResult.Fail("Pick a loan.")
        Try
            _repository.CancelLoan(loanId)
        Catch ex As PayrollRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Payroll, "Loan.Cancel", "Loan", loanId.ToString(CultureInfo.InvariantCulture), String.Empty))
    End Function

    Public Function ListLoans(employeeId As Integer) As OperationResult(Of IReadOnlyList(Of LoanSummary))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of LoanSummary)).Fail(allowed.Message)
        If employeeId <= 0 Then Return OperationResult(Of IReadOnlyList(Of LoanSummary)).Fail("Pick an employee.")
        Return OperationResult(Of IReadOnlyList(Of LoanSummary)).Ok(_repository.ListLoans(employeeId))
    End Function

    Public Function GetSchedule(loanId As Integer) As OperationResult(Of IReadOnlyList(Of LoanInstallmentView))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of LoanInstallmentView)).Fail(allowed.Message)
        If loanId <= 0 Then Return OperationResult(Of IReadOnlyList(Of LoanInstallmentView)).Fail("Pick a loan.")
        Dim rows As IReadOnlyList(Of LoanInstallmentView) = _repository.ListInstallments(loanId)
        If rows.Count = 0 Then Return OperationResult(Of IReadOnlyList(Of LoanInstallmentView)).Fail("Loan not found.")
        Return OperationResult(Of IReadOnlyList(Of LoanInstallmentView)).Ok(rows)
    End Function

    Private Function FindType(loanTypeId As Integer) As LoanTypeItem
        Return _repository.ListTypes().FirstOrDefault(Function(t) t.LoanTypeId.HasValue AndAlso t.LoanTypeId.Value = loanTypeId)
    End Function

    Private Function CheckAccess(action As PermissionAction) As OperationResult
        If Not _entitlements.IsEnabled(PlanFeature.Loans) Then
            Return OperationResult.Fail("Loans are not part of your plan.")
        End If
        If Not _permissions.IsAllowed(ModuleKey.Payroll, action) Then
            Return OperationResult.Fail(NotAllowed)
        End If
        Return OperationResult.Ok()
    End Function

    Private Function CurrentUserId() As Integer?
        Dim user As UserSession = _session.Current
        Return If(user Is Nothing, CType(Nothing, Integer?), user.UserId)
    End Function
End Class
