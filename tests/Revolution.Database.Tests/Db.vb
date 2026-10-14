Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>
''' Test access to a migrated, disposable SQL Server database. Set HRMS_TEST_CONNECTION to a db_owner connection string.
''' Without it the database tests do nothing (they pass without checking). Set HRMS_REQUIRE_DB_TESTS=1 to make that a failure.
''' Never point this at a real database: the tests create companies and employees.
''' </summary>
Friend NotInheritable Class Db
    Private Sub New()
    End Sub

    Friend Shared ReadOnly ConnectionString As String = Environment.GetEnvironmentVariable("HRMS_TEST_CONNECTION")
    Friend Shared ReadOnly Required As Boolean = Environment.GetEnvironmentVariable("HRMS_REQUIRE_DB_TESTS") = "1"

    Friend Shared ReadOnly Property Available As Boolean
        Get
            Return Not String.IsNullOrWhiteSpace(ConnectionString)
        End Get
    End Property

    ''' <summary>
    ''' Opens a connection the way the app does: tenant context in SESSION_CONTEXT (read only), then optionally switches to a
    ''' database user without a login. runAs "rv_test_app" is the app role (EXECUTE only); "rv_test_reader" can SELECT the tables.
    ''' </summary>
    Friend Shared Function Open(Optional companyId As Integer? = Nothing, Optional developer As Boolean = False, Optional runAs As String = Nothing, Optional userId As Integer? = Nothing) As SqlConnection
        Dim builder As New SqlConnectionStringBuilder(ConnectionString) With {.Pooling = False}
        Dim conn As New SqlConnection(builder.ConnectionString)
        conn.Open()
        If userId.HasValue Then
            SetContext(conn, "UserID", userId.Value)
        End If
        If companyId.HasValue Then
            SetContext(conn, "CompanyID", companyId.Value)
        End If
        If developer Then
            SetContext(conn, "IsDeveloper", True)
        End If
        If Not String.IsNullOrEmpty(runAs) Then
            Using cmd = conn.CreateCommand()
                cmd.CommandText = "EXECUTE AS USER = '" & runAs.Replace("'", "''") & "'"
                cmd.ExecuteNonQuery()
            End Using
        End If
        Return conn
    End Function

    Friend Shared Function Scalar(conn As SqlConnection, sql As String) As Object
        Using cmd = conn.CreateCommand()
            cmd.CommandText = sql
            Return cmd.ExecuteScalar()
        End Using
    End Function

    Friend Shared Sub Execute(conn As SqlConnection, sql As String)
        Using cmd = conn.CreateCommand()
            cmd.CommandText = sql
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>Runs a stored procedure with the given parameters and returns the first column of every row of its first result.</summary>
    Friend Shared Function Proc(conn As SqlConnection, name As String, ParamArray args() As (Name As String, Value As Object)) As List(Of Object)
        Using cmd = conn.CreateCommand()
            cmd.CommandText = name
            cmd.CommandType = CommandType.StoredProcedure
            For Each arg In args
                cmd.Parameters.AddWithValue(arg.Name, If(arg.Value, DBNull.Value))
            Next
            Dim rows As New List(Of Object)()
            Using r = cmd.ExecuteReader()
                While r.Read()
                    rows.Add(r.GetValue(0))
                End While
            End Using
            Return rows
        End Using
    End Function

    Private Shared Sub SetContext(conn As SqlConnection, key As String, value As Object)
        Using cmd = conn.CreateCommand()
            cmd.CommandText = "sp_set_session_context"
            cmd.CommandType = CommandType.StoredProcedure
            cmd.Parameters.AddWithValue("@key", key)
            cmd.Parameters.AddWithValue("@value", value)
            cmd.Parameters.AddWithValue("@read_only", True)
            cmd.ExecuteNonQuery()
        End Using
    End Sub
End Class
