Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Small helpers shared by the repositories. All database access is stored procedures (ADR 0001).</summary>
Public NotInheritable Class StoredProcedure
    Private Sub New()
    End Sub

    Public Shared Function CreateCommand(conn As SqlConnection, name As String) As SqlCommand
        Dim cmd = conn.CreateCommand()
        cmd.CommandText = name
        cmd.CommandType = CommandType.StoredProcedure
        Return cmd
    End Function

    Public Shared Function DbValue(value As Object) As Object
        Return If(value, DBNull.Value)
    End Function

    Public Shared Function ReadNullableInt(reader As SqlDataReader, column As String) As Integer?
        Dim i = reader.GetOrdinal(column)
        If reader.IsDBNull(i) Then
            Return Nothing
        End If
        Return reader.GetInt32(i)
    End Function

    Public Shared Function ReadNullableUtc(reader As SqlDataReader, column As String) As DateTime?
        Dim i = reader.GetOrdinal(column)
        If reader.IsDBNull(i) Then
            Return Nothing
        End If
        Return DateTime.SpecifyKind(reader.GetDateTime(i), DateTimeKind.Utc)
    End Function
End Class
