Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' Salary structure use cases: company pay policy, salary components, effective-dated employee salary. Needs the BasicPayroll feature
''' and Payroll permissions (View to read, Edit for policy and component changes, Create for a new component or a salary revision).
''' Audit text never holds an amount.
''' </summary>
Public NotInheritable Class SalaryService
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const DateFormat As String = "dd-MM-yyyy"

    Private ReadOnly _repository As ISalaryRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext

    Public Sub New(repository As ISalaryRepository, permissions As IPermissionService, entitlements As IEntitlementService,
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

    Public Function GetPolicy() As OperationResult(Of PolicySettings)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of PolicySettings).Fail(allowed.Message)
        Return OperationResult(Of PolicySettings).Ok(_repository.GetPolicy())
    End Function

    Public Function SavePolicy(policy As PolicySettings) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return allowed
        Dim check As OperationResult = SalaryValidator.ValidatePolicy(policy)
        If Not check.Succeeded Then Return check
        Try
            _repository.SavePolicy(policy, CurrentUserId())
        Catch ex As PayrollRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Payroll, "SavePolicy", "CompanyPolicy", String.Empty,
                                            $"Pay cycle starts on day {policy.PayCycleStartDay.ToString(CultureInfo.InvariantCulture)}."))
    End Function

    Public Function ListComponents() As OperationResult(Of IReadOnlyList(Of SalaryComponentItem))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of SalaryComponentItem)).Fail(allowed.Message)
        Return OperationResult(Of IReadOnlyList(Of SalaryComponentItem)).Ok(_repository.ListComponents())
    End Function

    ''' <summary>Adds a component (Create) or changes one (Edit). Returns the component id.</summary>
    Public Function SaveComponent(component As SalaryComponentItem) As OperationResult(Of Integer)
        Dim isChange As Boolean = component IsNot Nothing AndAlso component.ComponentId.HasValue
        Dim allowed As OperationResult = CheckAccess(If(isChange, PermissionAction.Edit, PermissionAction.Create))
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        Dim check As OperationResult = SalaryValidator.ValidateComponent(component)
        If Not check.Succeeded Then Return OperationResult(Of Integer).Fail(check.Message)
        Dim cleaned As New SalaryComponentItem(component.ComponentId, component.Code.Trim(), component.Name.Trim(), component.IsBasic,
                                               component.IsDa, component.IsOtBase, component.SortOrder, component.IsActive, component.IsHra)
        Dim id As Integer
        Try
            id = _repository.SaveComponent(cleaned)
        Catch ex As PayrollRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Payroll, If(isChange, "EditComponent", "AddComponent"),
                                                                      "SalaryComponent", id.ToString(CultureInfo.InvariantCulture), $"Code {cleaned.Code}."))
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    ''' <summary>The salary in force on a date. The result value is Nothing when the employee has no salary yet.</summary>
    Public Function GetSalaryAt(employeeId As Integer, asOf As Date) As OperationResult(Of EmployeeSalaryView)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of EmployeeSalaryView).Fail(allowed.Message)
        If employeeId <= 0 Then Return OperationResult(Of EmployeeSalaryView).Fail("Pick an employee.")
        Return OperationResult(Of EmployeeSalaryView).Ok(_repository.GetSalaryAt(employeeId, asOf.Date))
    End Function

    ''' <summary>Adds a salary revision from a date. Past revisions are kept. Returns the new salary id.</summary>
    Public Function SaveSalary(employeeId As Integer, effectiveFrom As Date, reason As String, lines As IReadOnlyList(Of SalaryLineWrite)) As OperationResult(Of Integer)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Create)
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        Dim components As IReadOnlyList(Of SalaryComponentItem) = _repository.ListComponents()
        Dim check As OperationResult = SalaryValidator.ValidateSalary(employeeId, effectiveFrom, reason, lines, components)
        If Not check.Succeeded Then Return OperationResult(Of Integer).Fail(check.Message)
        Dim id As Integer
        Try
            id = _repository.SaveSalary(employeeId, effectiveFrom.Date, If(reason Is Nothing, Nothing, reason.Trim()), lines, CurrentUserId())
        Catch ex As PayrollRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Payroll, "SaveSalary", "EmployeeSalary",
                                                                      employeeId.ToString(CultureInfo.InvariantCulture),
                                                                      $"From {effectiveFrom.ToString(DateFormat, CultureInfo.InvariantCulture)}; {lines.Count.ToString(CultureInfo.InvariantCulture)} lines."))
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    Private Function CheckAccess(action As PermissionAction) As OperationResult
        If Not _entitlements.IsEnabled(PlanFeature.BasicPayroll) Then
            Return OperationResult.Fail("Payroll is not part of your plan.")
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
