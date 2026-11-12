Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>
''' Asset recoveries (lost or damaged items) through ast.usp_AssetRecovery_RunDue, _RunReserve and _RunRelease (ADR 0001, ADR 0008). It speaks the same interface as the arrears ledger so a regular run takes both;
''' CompositeRecoveryLedger puts them together. Tenant context comes from IConnectionFactory.
''' </summary>
Public NotInheritable Class SqlAssetRecoveryLedger
    Implements IArrearsRecoveryLedger

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    ''' <summary>The open recoveries and the ones this run holds. ClaimId is the recovery id.</summary>
    Public Function Due(runId As Integer) As IReadOnlyList(Of ArrearsRecoveryDue) Implements IArrearsRecoveryLedger.Due
        Dim items As New List(Of ArrearsRecoveryDue)()
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_AssetRecovery_RunDue")
                    cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            items.Add(New ArrearsRecoveryDue(reader.GetInt32(reader.GetOrdinal("RecoveryID")), reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                                             reader.GetString(reader.GetOrdinal("EmpCode")), reader.GetDecimal(reader.GetOrdinal("Amount"))))
                        End While
                    End Using
                End Using
            End Using
        Catch ex As SqlException When AssetPayMessages.Translate(ex.Number) IsNot Nothing
            Throw New PayrollRuleException(AssetPayMessages.Translate(ex.Number), ex.Number, ex)
        End Try
        Return items
    End Function

    Public Sub Reserve(runId As Integer, claimIds As IReadOnlyCollection(Of Integer)) Implements IArrearsRecoveryLedger.Reserve
        If claimIds Is Nothing Then Throw New ArgumentNullException(NameOf(claimIds))
        Execute("ast.usp_AssetRecovery_RunReserve",
                Sub(cmd)
                    cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                    cmd.Parameters.Add("@RecoveryIdsJson", SqlDbType.NVarChar, -1).Value = System.Text.Json.JsonSerializer.Serialize(claimIds)
                End Sub)
    End Sub

    Public Sub Release(runId As Integer) Implements IArrearsRecoveryLedger.Release
        Execute("ast.usp_AssetRecovery_RunRelease", Sub(cmd) cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId)
    End Sub

    Private Sub Execute(procedure As String, bind As Action(Of SqlCommand))
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, procedure)
                    bind(cmd)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When AssetPayMessages.Translate(ex.Number) IsNot Nothing
            Throw New PayrollRuleException(AssetPayMessages.Translate(ex.Number), ex.Number, ex)
        End Try
    End Sub
End Class
