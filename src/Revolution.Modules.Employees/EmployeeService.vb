Imports System.Globalization
Imports Revolution.Core

''' <summary>Employee use cases: permission, validation, storage, audit. Returns OperationResult; never shows a message. Ids go through EmployeeIdentifierService.</summary>
Public NotInheritable Class EmployeeService
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const NotFound As String = "Employee not found."
    Private Const MaxSearchLength As Integer = 100

    Private ReadOnly _repository As IEmployeeRepository
    Private ReadOnly _identifiers As EmployeeIdentifierService
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _today As Func(Of Date)

    Public Sub New(repository As IEmployeeRepository, identifiers As EmployeeIdentifierService, permissions As IPermissionService,
                   audit As IAuditService, session As ISessionContext, today As Func(Of Date))
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If identifiers Is Nothing Then Throw New ArgumentNullException(NameOf(identifiers))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        If today Is Nothing Then Throw New ArgumentNullException(NameOf(today))
        _repository = repository
        _identifiers = identifiers
        _permissions = permissions
        _audit = audit
        _session = session
        _today = today
    End Sub

    ''' <summary>
    ''' Creates an employee with the next code. The ids on the draft are saved right after, in a second step.
    ''' If the employee is saved but an id is refused (for example a duplicate PAN), the result succeeds and IdWarning says why.
    ''' Bank details and statutory flags are not saved here; use EmployeeBankService and EmployeeStatutoryService.
    ''' </summary>
    Public Function Create(draft As EmployeeDraft, placement As EmployeePlacement) As OperationResult(Of NewEmployee)
        If Not _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.Create) Then
            Return OperationResult(Of NewEmployee).Fail(NotAllowed)
        End If
        Dim found = CheckInput(draft, placement)
        If found.Length > 0 Then
            Return OperationResult(Of NewEmployee).Fail(found)
        End If
        Dim created As NewEmployee
        Try
            created = _repository.Create(draft, placement, CurrentUserId())
        Catch ex As EmployeeRuleException
            Return OperationResult(Of NewEmployee).Fail(ex.Message)
        End Try
        Dim idResult = _identifiers.SaveCore(created.EmployeeId, IdentifierInput.FromDraft(draft))
        Dim recorded = _audit.Record(New AuditEntry(ModuleKey.Employees, "Create", "Employee", Key(created.EmployeeId), $"Code {created.EmpCode}"))
        If Not recorded.Succeeded Then
            Return OperationResult(Of NewEmployee).Fail($"Employee {created.EmpCode} was created, but the audit entry failed: {recorded.Message}")
        End If
        Return OperationResult(Of NewEmployee).Ok(New NewEmployee(created.EmployeeId, created.EmpCode, If(idResult.Succeeded, String.Empty, idResult.Message)))
    End Function

    ''' <summary>Updates everything except the ids. Pass the RowVersion from Get; a stale one is refused. Ids change only through EmployeeIdentifierService.</summary>
    Public Function Update(employeeId As Integer, rowVersion As Byte(), draft As EmployeeDraft, placement As EmployeePlacement) As OperationResult
        If Not _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.Edit) Then
            Return OperationResult.Fail(NotAllowed)
        End If
        If employeeId <= 0 Then
            Return OperationResult.Fail("Pick an employee.")
        End If
        Dim found = CheckInput(draft, placement)
        If found.Length > 0 Then
            Return OperationResult.Fail(found)
        End If
        Try
            _repository.Update(employeeId, rowVersion, draft, placement, CurrentUserId())
        Catch ex As EmployeeRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Employees, "Update", "Employee", Key(employeeId)))
    End Function

    Public Function [Get](employeeId As Integer) As OperationResult(Of EmployeeDetail)
        If Not _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.View) Then
            Return OperationResult(Of EmployeeDetail).Fail(NotAllowed)
        End If
        If employeeId <= 0 Then
            Return OperationResult(Of EmployeeDetail).Fail("Pick an employee.")
        End If
        Dim found = _repository.GetById(employeeId)
        Return If(found Is Nothing, OperationResult(Of EmployeeDetail).Fail(NotFound), OperationResult(Of EmployeeDetail).Ok(found))
    End Function

    Public Function List(search As String, factoryId As Integer?, activeOnly As Boolean) As OperationResult(Of IReadOnlyList(Of EmployeeSummary))
        If Not _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.View) Then
            Return OperationResult(Of IReadOnlyList(Of EmployeeSummary)).Fail(NotAllowed)
        End If
        Dim text As String = If(search, String.Empty).Trim()
        If text.Length > MaxSearchLength Then
            Return OperationResult(Of IReadOnlyList(Of EmployeeSummary)).Fail("The search text is too long.")
        End If
        Return OperationResult(Of IReadOnlyList(Of EmployeeSummary)).Ok(_repository.List(text, factoryId, activeOnly))
    End Function

    ''' <summary>Records that an employee left, retired, died or absconded. The exit date cannot be before the joining date.</summary>
    Public Function RecordExit(employeeId As Integer, exitDate As Date, status As EmployeeStatus) As OperationResult
        If Not _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.Edit) Then
            Return OperationResult.Fail(NotAllowed)
        End If
        If status < EmployeeStatus.Left OrElse status > EmployeeStatus.Absconded Then
            Return OperationResult.Fail("Exit status must be Left, Retired, Deceased or Absconded.")
        End If
        Dim current = _repository.GetById(employeeId)
        If current Is Nothing Then
            Return OperationResult.Fail(NotFound)
        End If
        If Not current.IsActive Then
            Return OperationResult.Fail("This employee has already left.")
        End If
        If exitDate.Date < current.JoinDate.Date Then
            Return OperationResult.Fail("Exit date cannot be before the joining date.")
        End If
        Return Execute(Sub() _repository.RecordExit(employeeId, exitDate, status, CurrentUserId()),
                       New AuditEntry(ModuleKey.Employees, "Exit", "Employee", Key(employeeId),
                                      $"{status} on {exitDate.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)}"))
    End Function

    Public Function Reactivate(employeeId As Integer) As OperationResult
        If Not _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.Edit) Then
            Return OperationResult.Fail(NotAllowed)
        End If
        Dim current = _repository.GetById(employeeId)
        If current Is Nothing Then
            Return OperationResult.Fail(NotFound)
        End If
        If current.IsActive Then
            Return OperationResult.Fail("This employee is already active.")
        End If
        Return Execute(Sub() _repository.Reactivate(employeeId, CurrentUserId()),
                       New AuditEntry(ModuleKey.Employees, "Reactivate", "Employee", Key(employeeId)))
    End Function

    Private Function Execute(work As Action, entry As AuditEntry) As OperationResult
        Try
            work()
        Catch ex As EmployeeRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(entry)
    End Function

    Private Function CheckInput(draft As EmployeeDraft, placement As EmployeePlacement) As String
        Dim all As New List(Of String)(EmployeeValidator.Validate(draft, _today()))
        all.AddRange(PlacementValidator.Validate(placement))
        Return String.Join(" ", all)
    End Function

    Private Shared Function Key(employeeId As Integer) As String
        Return employeeId.ToString(CultureInfo.InvariantCulture)
    End Function

    Private Function CurrentUserId() As Integer?
        Dim user = _session.Current
        Return If(user Is Nothing, CType(Nothing, Integer?), user.UserId)
    End Function
End Class
