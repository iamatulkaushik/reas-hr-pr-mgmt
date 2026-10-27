Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' Full and final use cases: save the calculated lines, approve, pay and reopen. Needs the FullAndFinal plan feature and Payroll permissions: View to read, Edit to
''' save lines, Approve to approve or pay, Reopen to send an approved settlement back. The person who calculated it cannot approve it. Audit text holds no amounts.
''' </summary>
Public NotInheritable Class FnfService
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const NoSettlement As String = "Calculate the settlement first."

    Private ReadOnly _repository As IFnfRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext

    Public Sub New(repository As IFnfRepository, permissions As IPermissionService, entitlements As IEntitlementService,
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

    ''' <summary>The settlement for a separation. Succeeds with Nothing when none is saved yet.</summary>
    Public Function GetSettlement(separationId As Integer) As OperationResult(Of FnfSettlementView)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of FnfSettlementView).Fail(allowed.Message)
        If separationId <= 0 Then Return OperationResult(Of FnfSettlementView).Fail("Pick a separation.")
        Return OperationResult(Of FnfSettlementView).Ok(_repository.Get(separationId))
    End Function

    ''' <summary>Totals for lines the screen has not saved yet.</summary>
    Public Function Preview(lines As IReadOnlyList(Of FnfLine)) As OperationResult(Of FnfTotals)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of FnfTotals).Fail(allowed.Message)
        If lines Is Nothing OrElse lines.Count = 0 Then Return OperationResult(Of FnfTotals).Fail("Add at least one line.")
        Return OperationResult(Of FnfTotals).Ok(FnfCalculator.Total(lines))
    End Function

    ''' <summary>Stores the lines and moves the settlement to Calculated. Allowed until it is approved. Returns the settlement id.</summary>
    Public Function SaveLines(separationId As Integer, lines As IReadOnlyList(Of FnfLine)) As OperationResult(Of Integer)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        If separationId <= 0 Then Return OperationResult(Of Integer).Fail("Pick a separation.")
        If lines Is Nothing OrElse lines.Count = 0 Then Return OperationResult(Of Integer).Fail("Add at least one line.")
        Dim current As FnfSettlementView = _repository.Get(separationId)
        If current IsNot Nothing AndAlso Not FnfTransition.CanEdit(current.Status) Then Return OperationResult(Of Integer).Fail("An approved settlement cannot be changed. Reopen it first.")
        Dim id As Integer
        Try
            id = _repository.Save(separationId, lines)
        Catch ex As PayrollRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Payroll, "Fnf.Calculate", "FnfSettlement", id.ToString(CultureInfo.InvariantCulture),
                                                                      $"{lines.Count.ToString(CultureInfo.InvariantCulture)} lines."))
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    Public Function Approve(separationId As Integer) As OperationResult
        Return Move(separationId, FnfStatus.Approved, PermissionAction.Approve, "Fnf.Approve")
    End Function

    Public Function MarkPaid(separationId As Integer) As OperationResult
        Return Move(separationId, FnfStatus.Paid, PermissionAction.Approve, "Fnf.Pay")
    End Function

    ''' <summary>Sends an approved settlement back to Calculated so the lines can change.</summary>
    Public Function Reopen(separationId As Integer) As OperationResult
        Return Move(separationId, FnfStatus.Calculated, PermissionAction.Reopen, "Fnf.Reopen")
    End Function

    Private Function Move(separationId As Integer, target As FnfStatus, action As PermissionAction, auditAction As String) As OperationResult
        Dim allowed As OperationResult = CheckAccess(action)
        If Not allowed.Succeeded Then Return allowed
        If separationId <= 0 Then Return OperationResult.Fail("Pick a separation.")
        Dim current As FnfSettlementView = _repository.Get(separationId)
        If current Is Nothing Then Return OperationResult.Fail(NoSettlement)
        If Not FnfTransition.CanMove(current.Status, target) Then Return OperationResult.Fail($"A {current.Status} settlement cannot move to {target}.")
        Dim userId As Integer? = CurrentUserId()
        If target = FnfStatus.Approved AndAlso (Not userId.HasValue OrElse Not FnfTransition.CanApprove(current.MakerUserId, userId.Value)) Then
            Return OperationResult.Fail("The person who calculated the settlement cannot approve it.")
        End If
        Try
            _repository.ChangeStatus(current.FnfId, target)
        Catch ex As PayrollRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Payroll, auditAction, "FnfSettlement", current.FnfId.ToString(CultureInfo.InvariantCulture), String.Empty))
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
