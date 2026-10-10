Imports System.Data

Public NotInheritable Class SqlAuditRepository
    Implements IAuditRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        _factory = factory
    End Sub

    Public Sub Insert(entry As AuditEntry, userId As Integer?, companyId As Integer?) Implements IAuditRepository.Insert
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "aud.usp_Audit_Insert")
                cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                cmd.Parameters.Add("@CompanyID", SqlDbType.Int).Value = StoredProcedure.DbValue(companyId)
                cmd.Parameters.Add("@ModuleID", SqlDbType.TinyInt).Value = CByte(CInt(entry.[Module]))
                cmd.Parameters.Add("@Action", SqlDbType.NVarChar, 100).Value = entry.Action
                cmd.Parameters.Add("@EntityName", SqlDbType.NVarChar, 100).Value = entry.EntityName
                cmd.Parameters.Add("@EntityID", SqlDbType.NVarChar, 100).Value = entry.EntityId
                cmd.Parameters.Add("@Details", SqlDbType.NVarChar, 2000).Value = entry.Details
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub
End Class
