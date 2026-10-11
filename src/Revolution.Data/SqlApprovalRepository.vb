Imports System.Data
Imports Microsoft.Data.SqlClient

Public NotInheritable Class SqlApprovalRepository
    Implements IApprovalRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        _factory = factory
    End Sub

    Public Function Submit(moduleKey As ModuleKey, entityName As String, entityId As String, summary As String, submittedBy As Integer) As Integer Implements IApprovalRepository.Submit
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "sec.usp_Approval_Submit")
                cmd.Parameters.Add("@ModuleID", SqlDbType.TinyInt).Value = CByte(CInt(moduleKey))
                cmd.Parameters.Add("@EntityName", SqlDbType.NVarChar, 100).Value = entityName
                cmd.Parameters.Add("@EntityID", SqlDbType.NVarChar, 100).Value = entityId
                cmd.Parameters.Add("@Summary", SqlDbType.NVarChar, 500).Value = summary
                cmd.Parameters.Add("@SubmittedBy", SqlDbType.Int).Value = submittedBy
                Return Convert.ToInt32(cmd.ExecuteScalar())
            End Using
        End Using
    End Function

    Public Function Decide(approvalId As Integer, status As ApprovalStatus, decidedBy As Integer, comment As String) As Boolean Implements IApprovalRepository.Decide
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "sec.usp_Approval_Decide")
                cmd.Parameters.Add("@ApprovalID", SqlDbType.Int).Value = approvalId
                cmd.Parameters.Add("@Status", SqlDbType.TinyInt).Value = CByte(CInt(status))
                cmd.Parameters.Add("@DecidedBy", SqlDbType.Int).Value = decidedBy
                cmd.Parameters.Add("@Comment", SqlDbType.NVarChar, 500).Value = StoredProcedure.DbValue(If(String.IsNullOrEmpty(comment), Nothing, comment))
                Return Convert.ToInt32(cmd.ExecuteScalar()) > 0
            End Using
        End Using
    End Function

    Public Function GetById(approvalId As Integer) As ApprovalRequest Implements IApprovalRepository.GetById
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "sec.usp_Approval_GetById")
                cmd.Parameters.Add("@ApprovalID", SqlDbType.Int).Value = approvalId
                Return ReadFirst(cmd)
            End Using
        End Using
    End Function

    Public Function GetLatest(moduleKey As ModuleKey, entityName As String, entityId As String) As ApprovalRequest Implements IApprovalRepository.GetLatest
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "sec.usp_Approval_GetLatest")
                cmd.Parameters.Add("@ModuleID", SqlDbType.TinyInt).Value = CByte(CInt(moduleKey))
                cmd.Parameters.Add("@EntityName", SqlDbType.NVarChar, 100).Value = entityName
                cmd.Parameters.Add("@EntityID", SqlDbType.NVarChar, 100).Value = entityId
                Return ReadFirst(cmd)
            End Using
        End Using
    End Function

    Public Function ListPending(moduleKey As ModuleKey) As IReadOnlyList(Of ApprovalRequest) Implements IApprovalRepository.ListPending
        Dim items As New List(Of ApprovalRequest)()
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "sec.usp_Approval_ListPending")
                cmd.Parameters.Add("@ModuleID", SqlDbType.TinyInt).Value = CByte(CInt(moduleKey))
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(Map(reader))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Private Shared Function ReadFirst(cmd As SqlCommand) As ApprovalRequest
        Using reader = cmd.ExecuteReader()
            Return If(reader.Read(), Map(reader), Nothing)
        End Using
    End Function

    Private Shared Function Map(r As SqlDataReader) As ApprovalRequest
        Dim decidedOn = StoredProcedure.ReadNullableUtc(r, "DecidedOn")
        Dim decidedByName = If(r.IsDBNull(r.GetOrdinal("DecidedByName")), String.Empty, r.GetString(r.GetOrdinal("DecidedByName")))
        Dim comment = If(r.IsDBNull(r.GetOrdinal("DecisionComment")), String.Empty, r.GetString(r.GetOrdinal("DecisionComment")))
        Return New ApprovalRequest(
            r.GetInt32(r.GetOrdinal("ApprovalID")),
            CType(CInt(r.GetByte(r.GetOrdinal("ModuleID"))), ModuleKey),
            r.GetString(r.GetOrdinal("EntityName")),
            r.GetString(r.GetOrdinal("EntityID")),
            r.GetString(r.GetOrdinal("Summary")),
            CType(CInt(r.GetByte(r.GetOrdinal("Status"))), ApprovalStatus),
            r.GetInt32(r.GetOrdinal("SubmittedBy")),
            r.GetString(r.GetOrdinal("SubmittedByName")),
            DateTime.SpecifyKind(r.GetDateTime(r.GetOrdinal("SubmittedOn")), DateTimeKind.Utc),
            StoredProcedure.ReadNullableInt(r, "DecidedBy"),
            decidedByName,
            decidedOn,
            comment)
    End Function
End Class
