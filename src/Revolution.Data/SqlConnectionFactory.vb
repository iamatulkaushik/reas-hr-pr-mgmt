Imports Microsoft.Data.SqlClient

''' <summary>Opens connections and sets SESSION_CONTEXT so row-level security sees the signed-in tenant.</summary>
Public NotInheritable Class SqlConnectionFactory
    Implements IConnectionFactory

    Private ReadOnly _connectionString As String
    Private ReadOnly _session As ISessionContext

    Public Sub New(connectionString As String, session As ISessionContext)
        If String.IsNullOrWhiteSpace(connectionString) Then
            Throw New ArgumentException("A connection string is required.", NameOf(connectionString))
        End If
        If session Is Nothing Then
            Throw New ArgumentNullException(NameOf(session))
        End If
        _connectionString = connectionString
        _session = session
    End Sub

    Public Function Open() As SqlConnection Implements IConnectionFactory.Open
        Dim conn As New SqlConnection(_connectionString)
        Try
            conn.Open()
            ApplyTenantContext(conn)
            Return conn
        Catch
            conn.Dispose()
            Throw
        End Try
    End Function

    Private Sub ApplyTenantContext(conn As SqlConnection)
        Dim user = _session.Current
        If user Is Nothing Then
            Return
        End If
        SetContext(conn, "UserID", user.UserId)
        SetContext(conn, "CompanyID", user.CompanyId)
        SetContext(conn, "IsDeveloper", user.Role = UserRole.Developer)
    End Sub

    Private Shared Sub SetContext(conn As SqlConnection, key As String, value As Object)
        Using cmd = StoredProcedure.CreateCommand(conn, "sp_set_session_context")
            cmd.Parameters.AddWithValue("@key", key)
            cmd.Parameters.AddWithValue("@value", value)
            cmd.Parameters.AddWithValue("@read_only", True)
            cmd.ExecuteNonQuery()
        End Using
    End Sub
End Class
