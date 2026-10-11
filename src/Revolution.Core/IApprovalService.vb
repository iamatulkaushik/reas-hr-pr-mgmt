''' <summary>
''' Maker-checker hook for actions that need a second person (payroll run, month reopen later). Modules ask IsRequired
''' and IsCleared; they never check a plan or role. Where approval is not required, IsCleared is always True.
''' </summary>
Public Interface IApprovalService
    ''' <summary>True when the signed-in plan needs a second approver for this module.</summary>
    Function IsRequired(moduleKey As ModuleKey) As Boolean

    ''' <summary>Asks for approval. The caller needs Edit on the module. Returns the request id.</summary>
    Function Submit(moduleKey As ModuleKey, entityName As String, entityId As String, summary As String) As OperationResult(Of Integer)

    ''' <summary>Approves a pending request. Needs Approve on the module, and the approver must not be the submitter.</summary>
    Function Approve(approvalId As Integer, Optional comment As String = "") As OperationResult

    ''' <summary>Rejects a pending request. Same rules as Approve, and a reason of 10 or more characters is required.</summary>
    Function Reject(approvalId As Integer, reason As String) As OperationResult

    ''' <summary>The submitter withdraws their own pending request.</summary>
    Function Cancel(approvalId As Integer) As OperationResult

    ''' <summary>True when approval is not required, or the newest request for the entity is Approved.</summary>
    Function IsCleared(moduleKey As ModuleKey, entityName As String, entityId As String) As Boolean

    Function GetLatest(moduleKey As ModuleKey, entityName As String, entityId As String) As ApprovalRequest

    ''' <summary>Pending requests for a module. Needs Approve on the module; otherwise empty.</summary>
    Function ListPending(moduleKey As ModuleKey) As IReadOnlyList(Of ApprovalRequest)
End Interface
