Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Modules.Biometric

''' <summary>
''' The agent's connection to the server (change request 0023, accepted by Sigma 2026-11-07): two procedures in the agt schema, agt.usp_Agent_Devices(@AgentKey) and
''' agt.usp_Agent_Ingest(@AgentKey, @DeviceCode, @PunchesJson). The agent key is sent as text (base64 of the 32 random bytes). The server answers a wrong, unknown or retired key with one generic
''' error and nothing about companies, so every failure here becomes the same plain message. No key, host or punch is put into any message.
''' </summary>
Public NotInheritable Class SqlAgentGateway
    Implements IAgentGateway

    Private Const Refused As String = "The server did not accept the request."

    Private ReadOnly _connectionString As String
    Private ReadOnly _agentKey As String

    Public Sub New(connectionString As String, agentKey As String)
        If String.IsNullOrWhiteSpace(connectionString) Then Throw New ArgumentException("Give the connection string.", NameOf(connectionString))
        If String.IsNullOrWhiteSpace(agentKey) Then Throw New ArgumentException("Give the agent key.", NameOf(agentKey))
        _connectionString = connectionString
        _agentKey = agentKey
    End Sub

    Public Function ListDevices() As IReadOnlyList(Of DeviceEndpoint) Implements IAgentGateway.ListDevices
        Dim devices As New List(Of DeviceEndpoint)()
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using cmd As SqlCommand = Command(conn, "agt.usp_Agent_Devices")
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim last As Integer = reader.GetOrdinal("LastPunchTime")
                            devices.Add(New DeviceEndpoint(0, reader.GetString(reader.GetOrdinal("DeviceCode")), reader.GetString(reader.GetOrdinal("Brand")),
                                                           reader.GetString(reader.GetOrdinal("Host")), reader.GetInt32(reader.GetOrdinal("Port")),
                                                           If(reader.IsDBNull(last), CType(Nothing, Date?), reader.GetDateTime(last))))
                        End While
                    End Using
                End Using
            End Using
        Catch ex As SqlException
            Throw New BiometricRuleException(Refused, ex.Number, ex)
        End Try
        Return devices
    End Function

    Public Function Ingest(deviceCode As String, punches As IReadOnlyList(Of RawPunch)) As PunchIngestResult Implements IPunchSink.Ingest
        If String.IsNullOrWhiteSpace(deviceCode) Then Throw New ArgumentException("Give the device code.", NameOf(deviceCode))
        If punches Is Nothing Then Throw New ArgumentNullException(NameOf(punches))
        Try
            Using conn As New SqlConnection(_connectionString)
                conn.Open()
                Using cmd As SqlCommand = Command(conn, "agt.usp_Agent_Ingest")
                    cmd.Parameters.Add("@DeviceCode", SqlDbType.NVarChar, 30).Value = deviceCode
                    cmd.Parameters.Add("@PunchesJson", SqlDbType.NVarChar, -1).Value = PunchesJson.Build(punches)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        If Not reader.Read() Then Return New PunchIngestResult(0, 0, 0)
                        Return New PunchIngestResult(reader.GetInt32(reader.GetOrdinal("Received")), reader.GetInt32(reader.GetOrdinal("Inserted")), reader.GetInt32(reader.GetOrdinal("Unmatched")))
                    End Using
                End Using
            End Using
        Catch ex As SqlException
            Throw New BiometricRuleException(Refused, ex.Number, ex)
        End Try
    End Function

    Private Function Command(conn As SqlConnection, procedure As String) As SqlCommand
        Dim cmd As New SqlCommand(procedure, conn) With {.CommandType = CommandType.StoredProcedure, .CommandTimeout = 60}
        cmd.Parameters.Add("@AgentKey", SqlDbType.NVarChar, 100).Value = _agentKey
        Return cmd
    End Function
End Class
