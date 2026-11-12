Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Asset facts for a settlement through ast.usp_AssetRecovery_FnfDue, _FnfUncleared, _FnfReserve and _FnfRelease (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlFnfAssetSource
    Implements IFnfAssetSource

    Private Const MoneyPrecision As Byte = 14
    Private Const MoneyScale As Byte = 2

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function Load(employeeId As Integer, fnfId As Integer?) As FnfAssetFacts Implements IFnfAssetSource.Load
        Dim total As Decimal = 0D
        Dim tags As New List(Of String)()
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_AssetRecovery_FnfDue")
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                    cmd.Parameters.Add("@FnfID", SqlDbType.Int).Value = StoredProcedure.DbValue(fnfId)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            total += reader.GetDecimal(reader.GetOrdinal("Amount"))
                        End While
                    End Using
                End Using
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_AssetRecovery_FnfUncleared")
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            tags.Add(reader.GetString(reader.GetOrdinal("TagNo")))
                        End While
                    End Using
                End Using
            End Using
        Catch ex As SqlException When AssetPayMessages.Translate(ex.Number) IsNot Nothing
            Throw New PayrollRuleException(AssetPayMessages.Translate(ex.Number), ex.Number, ex)
        End Try
        Return New FnfAssetFacts(total, tags)
    End Function

    Public Sub Reserve(fnfId As Integer, employeeId As Integer, total As Decimal) Implements IFnfAssetSource.Reserve
        Execute("ast.usp_AssetRecovery_FnfReserve",
                Sub(cmd)
                    cmd.Parameters.Add("@FnfID", SqlDbType.Int).Value = fnfId
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                    cmd.Parameters.Add(New SqlParameter("@Total", SqlDbType.Decimal) With {.Precision = MoneyPrecision, .Scale = MoneyScale, .Value = total})
                End Sub)
    End Sub

    Public Sub Release(fnfId As Integer) Implements IFnfAssetSource.Release
        Execute("ast.usp_AssetRecovery_FnfRelease", Sub(cmd) cmd.Parameters.Add("@FnfID", SqlDbType.Int).Value = fnfId)
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
