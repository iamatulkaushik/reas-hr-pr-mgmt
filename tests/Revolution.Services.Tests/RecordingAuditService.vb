Imports Revolution.Core

Public NotInheritable Class RecordingAuditService
    Implements IAuditService

    Public ReadOnly Property Entries As New List(Of AuditEntry)()

    Public Function Record(entry As AuditEntry) As OperationResult Implements IAuditService.Record
        Entries.Add(entry)
        Return OperationResult.Ok()
    End Function
End Class
