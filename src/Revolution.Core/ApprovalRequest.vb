''' <summary>One request for a second person to approve a business action, for example a payroll run.</summary>
Public NotInheritable Class ApprovalRequest
    Public Sub New(approvalId As Integer, moduleKey As ModuleKey, entityName As String, entityId As String, summary As String,
                   status As ApprovalStatus, submittedById As Integer, submittedByName As String, submittedOnUtc As DateTime,
                   decidedById As Integer?, decidedByName As String, decidedOnUtc As DateTime?, decisionComment As String)
        Me.ApprovalId = approvalId
        Me.[Module] = moduleKey
        Me.EntityName = entityName
        Me.EntityId = entityId
        Me.Summary = summary
        Me.Status = status
        Me.SubmittedById = submittedById
        Me.SubmittedByName = submittedByName
        Me.SubmittedOnUtc = submittedOnUtc
        Me.DecidedById = decidedById
        Me.DecidedByName = If(decidedByName, String.Empty)
        Me.DecidedOnUtc = decidedOnUtc
        Me.DecisionComment = If(decisionComment, String.Empty)
    End Sub

    Public ReadOnly Property ApprovalId As Integer
    Public ReadOnly Property [Module] As ModuleKey
    Public ReadOnly Property EntityName As String
    Public ReadOnly Property EntityId As String
    Public ReadOnly Property Summary As String
    Public ReadOnly Property Status As ApprovalStatus
    Public ReadOnly Property SubmittedById As Integer
    Public ReadOnly Property SubmittedByName As String
    Public ReadOnly Property SubmittedOnUtc As DateTime
    Public ReadOnly Property DecidedById As Integer?
    Public ReadOnly Property DecidedByName As String
    Public ReadOnly Property DecidedOnUtc As DateTime?
    Public ReadOnly Property DecisionComment As String
End Class
