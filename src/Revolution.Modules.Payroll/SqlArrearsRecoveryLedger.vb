Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Overpaid arrears claims through pay.usp_ArrearsRecovery_Due, _Reserve and _Release (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlArrearsRecoveryLedger
    Implements IArrearsRecoveryLedger

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function Due(runId As Integer) As IReadOnlyList(Of ArrearsRecoveryDue) Implements IArrearsRecoveryLedger.Due
        Dim items As New List(Of ArrearsRecoveryDue)()
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_ArrearsRecovery_Due")
                    cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            items.Add(New ArrearsRecoveryDue(reader.GetInt32(reader.GetOrdinal("ClaimID")), reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                                             reader.GetString(reader.GetOrdinal("EmpCode")), reader.GetDecimal(reader.GetOrdinal("Amount"))))
                        End While
                    End Using
                End Using
            End Using
        Catch ex As SqlException When PayrollErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(PayrollErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
        Return items
    End Function

    Public Sub Reserve(runId As Integer, claimIds As IReadOnlyCollection(Of Integer)) Implements IArrearsRecoveryLedger.Reserve
        If claimIds Is Nothing Then Throw New ArgumentNullException(NameOf(claimIds))
        Execute("pay.usp_ArrearsRecovery_Reserve",
                Sub(cmd)
                    cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                    cmd.Parameters.Add("@ClaimIdsJson", SqlDbType.NVarChar, -1).Value = System.Text.Json.JsonSerializer.Serialize(claimIds)
                End Sub)
    End Sub

    Public Sub Release(runId As Integer) Implements IArrearsRecoveryLedger.Release
        Execute("pay.usp_ArrearsRecovery_Release", Sub(cmd) cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId)
    End Sub

    Private Sub Execute(procedure As String, bind As Action(Of SqlCommand))
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, procedure)
                    bind(cmd)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When PayrollErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(PayrollErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
    End Sub
End Class
