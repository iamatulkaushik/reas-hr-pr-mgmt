Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Arrears recovery through pay.usp_ArrearsRecovery_Pending, _Apply and _Reverse (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlArrearsRecoveryStore
    Implements IArrearsRecoveryStore

    Private Const MismatchNumber As Integer = 53109
    Private Const MismatchText As String = "The arrears recoveries in the run do not match the open recovery claims. Process the run again."

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function PendingByEmployee() As IReadOnlyDictionary(Of Integer, Decimal) Implements IArrearsRecoveryStore.PendingByEmployee
        Dim result As New Dictionary(Of Integer, Decimal)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_ArrearsRecovery_Pending")
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        result(reader.GetInt32(reader.GetOrdinal("EmployeeID"))) = reader.GetDecimal(reader.GetOrdinal("Amount"))
                    End While
                End Using
            End Using
        End Using
        Return result
    End Function

    Public Sub Apply(runId As Integer) Implements IArrearsRecoveryStore.Apply
        Try
            Run("pay.usp_ArrearsRecovery_Apply", runId)
        Catch ex As SqlException When ex.Number = MismatchNumber
            Throw New PayrollRuleException(MismatchText, ex.Number, ex)
        End Try
    End Sub

    Public Sub Reverse(runId As Integer) Implements IArrearsRecoveryStore.Reverse
        Run("pay.usp_ArrearsRecovery_Reverse", runId)
    End Sub

    Private Sub Run(procedure As String, runId As Integer)
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, procedure)
                cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub
End Class
