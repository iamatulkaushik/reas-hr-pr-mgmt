Imports Revolution.Core

Public NotInheritable Class FakeApprovalRepository
    Implements IApprovalRepository

    Private ReadOnly _items As New List(Of ApprovalRequest)()
    Private ReadOnly _names As New Dictionary(Of Integer, String)()

    Public Sub AddUser(userId As Integer, userName As String)
        _names(userId) = userName
    End Sub

    Public Function Submit(moduleKey As ModuleKey, entityName As String, entityId As String, summary As String, submittedBy As Integer) As Integer Implements IApprovalRepository.Submit
        If _items.Any(Function(x) x.Module = moduleKey AndAlso x.EntityName = entityName AndAlso x.EntityId = entityId AndAlso x.Status = ApprovalStatus.Pending) Then
            Return 0
        End If
        Dim id = _items.Count + 1
        _items.Add(New ApprovalRequest(id, moduleKey, entityName, entityId, summary, ApprovalStatus.Pending, submittedBy,
                                       NameOf(submittedBy), DateTime.UtcNow, Nothing, Nothing, Nothing, Nothing))
        Return id
    End Function

    Public Function Decide(approvalId As Integer, status As ApprovalStatus, decidedBy As Integer, comment As String) As Boolean Implements IApprovalRepository.Decide
        Dim old = GetById(approvalId)
        If old Is Nothing OrElse old.Status <> ApprovalStatus.Pending Then
            Return False
        End If
        Dim sameUser = old.SubmittedById = decidedBy
        If (status = ApprovalStatus.Cancelled) <> sameUser Then
            Return False
        End If
        _items(approvalId - 1) = New ApprovalRequest(old.ApprovalId, old.Module, old.EntityName, old.EntityId, old.Summary, status,
                                                     old.SubmittedById, old.SubmittedByName, old.SubmittedOnUtc, decidedBy, "decider",
                                                     DateTime.UtcNow, comment)
        Return True
    End Function

    Public Function GetById(approvalId As Integer) As ApprovalRequest Implements IApprovalRepository.GetById
        Return _items.FirstOrDefault(Function(x) x.ApprovalId = approvalId)
    End Function

    Public Function GetLatest(moduleKey As ModuleKey, entityName As String, entityId As String) As ApprovalRequest Implements IApprovalRepository.GetLatest
        Return _items.LastOrDefault(Function(x) x.Module = moduleKey AndAlso x.EntityName = entityName AndAlso x.EntityId = entityId)
    End Function

    Public Function ListPending(moduleKey As ModuleKey) As IReadOnlyList(Of ApprovalRequest) Implements IApprovalRepository.ListPending
        Return _items.Where(Function(x) x.Module = moduleKey AndAlso x.Status = ApprovalStatus.Pending).ToList()
    End Function
End Class
