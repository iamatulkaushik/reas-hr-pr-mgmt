Imports Revolution.Core

Public NotInheritable Class FakeAuditRepository
    Implements IAuditRepository

    Public ReadOnly Property Entries As New List(Of AuditEntry)()
    Public Property LastUserId As Integer?
    Public Property LastCompanyId As Integer?

    Public Sub Insert(entry As AuditEntry, userId As Integer?, companyId As Integer?) Implements IAuditRepository.Insert
        Entries.Add(entry)
        LastUserId = userId
        LastCompanyId = companyId
    End Sub
End Class
