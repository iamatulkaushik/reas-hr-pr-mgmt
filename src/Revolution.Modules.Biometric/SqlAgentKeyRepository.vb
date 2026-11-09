Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Agent keys through sec.usp_Agent_Issue, usp_Agent_List and usp_Agent_Retire (ADR 0001, ADR 0008, ADR 0021). Tenant and user context come from IConnectionFactory.</summary>
Public NotInheritable Class SqlAgentKeyRepository
    Implements IAgentKeyRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function Issue(label As String) As IssuedAgentKey Implements IAgentKeyRepository.Issue
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "sec.usp_Agent_Issue")
                    cmd.Parameters.Add("@Label", SqlDbType.NVarChar, 60).Value = label
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        If Not reader.Read() Then Throw New BiometricRuleException("The key could not be issued.")
                        Return New IssuedAgentKey(reader.GetInt32(reader.GetOrdinal("AgentRegistrationID")), reader.GetString(reader.GetOrdinal("AgentKey")))
                    End Using
                End Using
            End Using
        Catch ex As SqlException When BiometricErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New BiometricRuleException(BiometricErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
    End Function

    Public Function List() As IReadOnlyList(Of AgentKeyItem) Implements IAgentKeyRepository.List
        Dim items As New List(Of AgentKeyItem)()
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "sec.usp_Agent_List")
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            items.Add(New AgentKeyItem(reader.GetInt32(reader.GetOrdinal("AgentRegistrationID")), reader.GetString(reader.GetOrdinal("Label")),
                                                       reader.GetBoolean(reader.GetOrdinal("IsActive")), reader.GetDateTime(reader.GetOrdinal("CreatedUtc")),
                                                       NullableDate(reader, "RetiredUtc"), NullableDate(reader, "LastSeenUtc")))
                        End While
                    End Using
                End Using
            End Using
        Catch ex As SqlException When BiometricErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New BiometricRuleException(BiometricErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
        Return items
    End Function

    Public Sub Retire(registrationId As Integer) Implements IAgentKeyRepository.Retire
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "sec.usp_Agent_Retire")
                    cmd.Parameters.Add("@AgentRegistrationID", SqlDbType.Int).Value = registrationId
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When BiometricErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New BiometricRuleException(BiometricErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
    End Sub

    Private Shared Function NullableDate(reader As SqlDataReader, name As String) As Date?
        Dim i As Integer = reader.GetOrdinal(name)
        Return If(reader.IsDBNull(i), CType(Nothing, Date?), reader.GetDateTime(i))
    End Function
End Class
