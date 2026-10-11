''' <summary>Maker-checker approvals. Only plans with DualApproval need them (ADR 0009).</summary>
Public NotInheritable Class ApprovalService
    Implements IApprovalService

    Private Const MaxSummaryLength As Integer = 500
    Private Const MinReasonLength As Integer = 10

    Private ReadOnly _repository As IApprovalRepository
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService

    Public Sub New(repository As IApprovalRepository, session As ISessionContext, permissions As IPermissionService,
                   entitlements As IEntitlementService, audit As IAuditService)
        _repository = repository
        _session = session
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
    End Sub

    Public Function IsRequired(moduleKey As ModuleKey) As Boolean Implements IApprovalService.IsRequired
        Return _entitlements.IsEnabled(PlanFeature.DualApproval)
    End Function

    Public Function Submit(moduleKey As ModuleKey, entityName As String, entityId As String, summary As String) As OperationResult(Of Integer) Implements IApprovalService.Submit
        Dim user = _session.Current
        If user Is Nothing Then
            Return OperationResult(Of Integer).Fail("Sign in first.")
        End If
        If Not IsRequired(moduleKey) Then
            Return OperationResult(Of Integer).Fail("Approval is not part of this plan.")
        End If
        If Not _permissions.IsAllowed(moduleKey, PermissionAction.Edit) Then
            Return OperationResult(Of Integer).Fail("You are not allowed to submit this for approval.")
        End If
        If String.IsNullOrWhiteSpace(entityName) OrElse String.IsNullOrWhiteSpace(entityId) Then
            Return OperationResult(Of Integer).Fail("Say what is being submitted.")
        End If
        If String.IsNullOrWhiteSpace(summary) OrElse summary.Length > MaxSummaryLength Then
            Return OperationResult(Of Integer).Fail("The summary is required and can be up to 500 characters.")
        End If

        Dim id = _repository.Submit(moduleKey, entityName, entityId, summary.Trim(), user.UserId)
        If id <= 0 Then
            Return OperationResult(Of Integer).Fail("An approval is already pending for this item.")
        End If
        _audit.Record(New AuditEntry(moduleKey, "ApprovalSubmitted", entityName, entityId, summary.Trim()))
        Return OperationResult(Of Integer).Ok(id)
    End Function

    Public Function Approve(approvalId As Integer, Optional comment As String = "") As OperationResult Implements IApprovalService.Approve
        Return Decide(approvalId, ApprovalStatus.Approved, comment, "ApprovalApproved")
    End Function

    Public Function Reject(approvalId As Integer, reason As String) As OperationResult Implements IApprovalService.Reject
        If String.IsNullOrWhiteSpace(reason) OrElse reason.Trim().Length < MinReasonLength Then
            Return OperationResult.Fail("Give a reason of at least 10 characters.")
        End If
        Return Decide(approvalId, ApprovalStatus.Rejected, reason, "ApprovalRejected")
    End Function

    Public Function Cancel(approvalId As Integer) As OperationResult Implements IApprovalService.Cancel
        Dim user = _session.Current
        If user Is Nothing Then
            Return OperationResult.Fail("Sign in first.")
        End If
        Dim request = _repository.GetById(approvalId)
        If request Is Nothing OrElse request.Status <> ApprovalStatus.Pending Then
            Return OperationResult.Fail("That request is not pending.")
        End If
        If request.SubmittedById <> user.UserId Then
            Return OperationResult.Fail("Only the person who submitted a request can cancel it.")
        End If
        If Not _repository.Decide(approvalId, ApprovalStatus.Cancelled, user.UserId, String.Empty) Then
            Return OperationResult.Fail("That request is no longer pending.")
        End If
        _audit.Record(New AuditEntry(request.Module, "ApprovalCancelled", request.EntityName, request.EntityId))
        Return OperationResult.Ok()
    End Function

    Public Function IsCleared(moduleKey As ModuleKey, entityName As String, entityId As String) As Boolean Implements IApprovalService.IsCleared
        If Not IsRequired(moduleKey) Then
            Return True
        End If
        Dim latest = _repository.GetLatest(moduleKey, entityName, entityId)
        Return latest IsNot Nothing AndAlso latest.Status = ApprovalStatus.Approved
    End Function

    Public Function GetLatest(moduleKey As ModuleKey, entityName As String, entityId As String) As ApprovalRequest Implements IApprovalService.GetLatest
        Return _repository.GetLatest(moduleKey, entityName, entityId)
    End Function

    Public Function ListPending(moduleKey As ModuleKey) As IReadOnlyList(Of ApprovalRequest) Implements IApprovalService.ListPending
        If Not _permissions.IsAllowed(moduleKey, PermissionAction.Approve) Then
            Return New List(Of ApprovalRequest)()
        End If
        Return _repository.ListPending(moduleKey)
    End Function

    Private Function Decide(approvalId As Integer, status As ApprovalStatus, comment As String, auditAction As String) As OperationResult
        Dim user = _session.Current
        If user Is Nothing Then
            Return OperationResult.Fail("Sign in first.")
        End If
        Dim request = _repository.GetById(approvalId)
        If request Is Nothing OrElse request.Status <> ApprovalStatus.Pending Then
            Return OperationResult.Fail("That request is not pending.")
        End If
        If Not _permissions.IsAllowed(request.Module, PermissionAction.Approve) Then
            Return OperationResult.Fail("You are not allowed to approve this.")
        End If
        If request.SubmittedById = user.UserId Then
            Return OperationResult.Fail("A different person must approve what you submitted.")
        End If
        Dim text = If(comment, String.Empty).Trim()
        If Not _repository.Decide(approvalId, status, user.UserId, text) Then
            Return OperationResult.Fail("That request is no longer pending.")
        End If
        _audit.Record(New AuditEntry(request.Module, auditAction, request.EntityName, request.EntityId, text))
        Return OperationResult.Ok()
    End Function
End Class
