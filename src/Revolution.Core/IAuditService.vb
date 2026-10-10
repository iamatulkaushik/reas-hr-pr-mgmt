''' <summary>Writes audit records. Stamps user, company and UTC time from the session.</summary>
Public Interface IAuditService
    Function Record(entry As AuditEntry) As OperationResult
End Interface
