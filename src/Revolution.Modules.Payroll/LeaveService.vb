Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' Leave use cases for full and final: leave types, ledger postings, balances and the encashment for an exit. Needs the FullAndFinal plan feature and Payroll
''' permissions: View to read, Create for a new type or a posting, Edit to change a type or post an Adjustment. The leave year is the financial year of the
''' entry date. The ledger is append only; a mistake is corrected with an Adjustment. Audit text holds no days or amounts.
''' </summary>
Public NotInheritable Class LeaveService
    Private Const NotAllowed As String = "You are not allowed to do this."

    Private ReadOnly _repository As ILeaveRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext

    Public Sub New(repository As ILeaveRepository, permissions As IPermissionService, entitlements As IEntitlementService,
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

    Public Function ListTypes() As OperationResult(Of IReadOnlyList(Of LeaveTypeItem))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of LeaveTypeItem)).Fail(allowed.Message)
        Return OperationResult(Of IReadOnlyList(Of LeaveTypeItem)).Ok(_repository.ListTypes())
    End Function

    ''' <summary>Adds a leave type (Create) or changes one (Edit). Returns the leave type id.</summary>
    Public Function SaveType(leaveType As LeaveTypeItem) As OperationResult(Of Integer)
        Dim isChange As Boolean = leaveType IsNot Nothing AndAlso leaveType.LeaveTypeId.HasValue
        Dim allowed As OperationResult = CheckAccess(If(isChange, PermissionAction.Edit, PermissionAction.Create))
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        Dim check As OperationResult = LeaveValidator.ValidateType(leaveType)
        If Not check.Succeeded Then Return OperationResult(Of Integer).Fail(check.Message)
        Dim cleaned As New LeaveTypeItem(leaveType.LeaveTypeId, leaveType.Code.Trim().ToUpperInvariant(), leaveType.Name.Trim(), leaveType.IsEncashable,
                                         leaveType.MaxEncashDays, leaveType.MaxCarryForwardDays, leaveType.IsActive)
        Dim id As Integer
        Try
            id = _repository.SaveType(cleaned)
        Catch ex As PayrollRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Payroll, If(isChange, "LeaveType.Edit", "LeaveType.Add"), "LeaveType",
                                                                      id.ToString(CultureInfo.InvariantCulture), $"Code {cleaned.Code}."))
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    ''' <summary>Adds a ledger row. Days is positive for every kind except Adjustment. Returns the ledger id.</summary>
    Public Function Post(employeeId As Integer, leaveTypeId As Integer, entryDate As Date, kind As LeaveEntryKind, days As Decimal, note As String) As OperationResult(Of Long)
        Dim allowed As OperationResult = CheckAccess(If(kind = LeaveEntryKind.Adjustment, PermissionAction.Edit, PermissionAction.Create))
        If Not allowed.Succeeded Then Return OperationResult(Of Long).Fail(allowed.Message)
        If employeeId <= 0 Then Return OperationResult(Of Long).Fail("Pick an employee.")
        Dim check As OperationResult = LeaveValidator.ValidatePosting(FindType(leaveTypeId), kind, days, entryDate, note)
        If Not check.Succeeded Then Return OperationResult(Of Long).Fail(check.Message)
        Dim id As Long
        Try
            id = _repository.Post(employeeId, leaveTypeId, FinancialYearCalendar.StartYear(entryDate), entryDate.Date, kind, days, If(note, String.Empty).Trim(), CurrentUserId())
        Catch ex As PayrollRuleException
            Return OperationResult(Of Long).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Payroll, "Leave.Post", "LeaveLedger", id.ToString(CultureInfo.InvariantCulture),
                                                                      $"Employee {employeeId.ToString(CultureInfo.InvariantCulture)}, {kind}."))
        If Not logged.Succeeded Then Return OperationResult(Of Long).Fail(logged.Message)
        Return OperationResult(Of Long).Ok(id)
    End Function

    Public Function ListBalances(employeeId As Integer) As OperationResult(Of IReadOnlyList(Of LeaveBalanceRow))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of LeaveBalanceRow)).Fail(allowed.Message)
        If employeeId <= 0 Then Return OperationResult(Of IReadOnlyList(Of LeaveBalanceRow)).Fail("Pick an employee.")
        Return OperationResult(Of IReadOnlyList(Of LeaveBalanceRow)).Ok(_repository.ListBalances(employeeId))
    End Function

    Public Function ListEntries(employeeId As Integer, leaveTypeId As Integer?) As OperationResult(Of IReadOnlyList(Of LeaveEntryView))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of LeaveEntryView)).Fail(allowed.Message)
        If employeeId <= 0 Then Return OperationResult(Of IReadOnlyList(Of LeaveEntryView)).Fail("Pick an employee.")
        Return OperationResult(Of IReadOnlyList(Of LeaveEntryView)).Ok(_repository.ListEntries(employeeId, leaveTypeId))
    End Function

    ''' <summary>What leaving would pay for this leave type: the balance cut to the type's encashment limit, times the daily wage. Nothing is saved.</summary>
    Public Function EncashmentFor(employeeId As Integer, leaveTypeId As Integer, dailyWage As Decimal) As OperationResult(Of LeaveEncashmentResult)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of LeaveEncashmentResult).Fail(allowed.Message)
        If employeeId <= 0 Then Return OperationResult(Of LeaveEncashmentResult).Fail("Pick an employee.")
        If dailyWage < 0D Then Return OperationResult(Of LeaveEncashmentResult).Fail("The daily wage cannot be negative.")
        Dim leaveType As LeaveTypeItem = FindType(leaveTypeId)
        If leaveType Is Nothing Then Return OperationResult(Of LeaveEncashmentResult).Fail("Pick a leave type.")
        If Not leaveType.IsEncashable Then Return OperationResult(Of LeaveEncashmentResult).Fail("This leave type cannot be encashed.")
        Dim row As LeaveBalanceRow = _repository.ListBalances(employeeId).FirstOrDefault(Function(b) b.LeaveTypeId = leaveTypeId)
        Dim balance As Decimal = If(row Is Nothing, 0D, Math.Max(0D, row.BalanceDays))
        Return OperationResult(Of LeaveEncashmentResult).Ok(LeaveEncashmentCalculator.Calculate(balance, If(leaveType.MaxEncashDays, 0D), dailyWage))
    End Function

    Private Function FindType(leaveTypeId As Integer) As LeaveTypeItem
        Return _repository.ListTypes().FirstOrDefault(Function(t) t.LeaveTypeId.HasValue AndAlso t.LeaveTypeId.Value = leaveTypeId)
    End Function

    Private Function CheckAccess(action As PermissionAction) As OperationResult
        If Not _entitlements.IsEnabled(PlanFeature.FullAndFinal) Then
            Return OperationResult.Fail("Full and final is not part of your plan.")
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
