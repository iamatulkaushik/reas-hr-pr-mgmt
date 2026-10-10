''' <summary>Audit storage. Append only.</summary>
Public Interface IAuditRepository
    Sub Insert(entry As AuditEntry, userId As Integer?, companyId As Integer?)
End Interface
