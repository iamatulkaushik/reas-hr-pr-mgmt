''' <summary>Approval request storage. The database enforces one pending request per entity and maker is not checker.</summary>
Public Interface IApprovalRepository
    ''' <summary>Creates a pending request. Returns its id, or 0 when one is already pending for the entity.</summary>
    Function Submit(moduleKey As ModuleKey, entityName As String, entityId As String, summary As String, submittedBy As Integer) As Integer

    ''' <summary>Moves a pending request to Approved, Rejected or Cancelled. False when it was no longer pending or the rule blocked it.</summary>
    Function Decide(approvalId As Integer, status As ApprovalStatus, decidedBy As Integer, comment As String) As Boolean

    Function GetById(approvalId As Integer) As ApprovalRequest

    ''' <summary>The newest request for the entity, or Nothing.</summary>
    Function GetLatest(moduleKey As ModuleKey, entityName As String, entityId As String) As ApprovalRequest

    Function ListPending(moduleKey As ModuleKey) As IReadOnlyList(Of ApprovalRequest)
End Interface
