Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Recovery storage through ast.usp_AssetRecovery_* (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlAssetRecoveryRepository
    Implements IAssetRecoveryRepository

    Private Const MoneyPrecision As Byte = 12
    Private Const MoneyScale As Byte = 2

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function Source(assetId As Integer) As AssetRecoverySource Implements IAssetRecoveryRepository.Source
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_AssetRecovery_Source")
                    cmd.Parameters.Add("@AssetID", SqlDbType.Int).Value = assetId
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        If Not reader.Read() Then Throw New AssetsRuleException(AssetsErrorMap.Translate(53505))
                        Dim issueAt As Integer = reader.GetOrdinal("IssueID")
                        Dim employeeAt As Integer = reader.GetOrdinal("EmployeeID")
                        Dim codeAt As Integer = reader.GetOrdinal("EmpCode")
                        Dim nameAt As Integer = reader.GetOrdinal("FullName")
                        Return New AssetRecoverySource(reader.GetInt32(reader.GetOrdinal("AssetID")), reader.GetString(reader.GetOrdinal("TagNo")),
                                                       reader.GetString(reader.GetOrdinal("TypeName")), reader.GetDateTime(reader.GetOrdinal("PurchaseDate")),
                                                       reader.GetDecimal(reader.GetOrdinal("PurchaseValue")), reader.GetInt32(reader.GetOrdinal("UsefulLifeMonths")),
                                                       CType(CInt(reader.GetByte(reader.GetOrdinal("StatusID"))), AssetStatus),
                                                       If(reader.IsDBNull(issueAt), CType(Nothing, Integer?), reader.GetInt32(issueAt)),
                                                       If(reader.IsDBNull(employeeAt), CType(Nothing, Integer?), reader.GetInt32(employeeAt)),
                                                       If(reader.IsDBNull(codeAt), String.Empty, reader.GetString(codeAt)),
                                                       If(reader.IsDBNull(nameAt), String.Empty, reader.GetString(nameAt)))
                    End Using
                End Using
            End Using
        Catch ex As SqlException When AssetsErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New AssetsRuleException(AssetsErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
    End Function

    Public Function Add(recovery As AssetRecoveryAdd, userId As Integer?) As Integer Implements IAssetRecoveryRepository.Add
        If recovery Is Nothing Then Throw New ArgumentNullException(NameOf(recovery))
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_AssetRecovery_Add")
                    cmd.Parameters.Add("@AssetID", SqlDbType.Int).Value = recovery.AssetId
                    cmd.Parameters.Add("@BasisDate", SqlDbType.Date).Value = recovery.BasisDate.Date
                    cmd.Parameters.Add(New SqlParameter("@DepreciatedValue", SqlDbType.Decimal) With {.Precision = MoneyPrecision, .Scale = MoneyScale, .Value = recovery.DepreciatedValue})
                    cmd.Parameters.Add(New SqlParameter("@Amount", SqlDbType.Decimal) With {.Precision = MoneyPrecision, .Scale = MoneyScale, .Value = recovery.Amount})
                    cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, 200).Value = If(String.IsNullOrEmpty(recovery.Reason), CObj(DBNull.Value), recovery.Reason)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    Return Convert.ToInt32(cmd.ExecuteScalar(), Globalization.CultureInfo.InvariantCulture)
                End Using
            End Using
        Catch ex As SqlException When AssetsErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New AssetsRuleException(AssetsErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
    End Function

    Public Sub Cancel(recoveryId As Integer, reason As String, userId As Integer?) Implements IAssetRecoveryRepository.Cancel
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_AssetRecovery_Cancel")
                    cmd.Parameters.Add("@RecoveryID", SqlDbType.Int).Value = recoveryId
                    cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, 200).Value = reason
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When AssetsErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New AssetsRuleException(AssetsErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
    End Sub

    Public Function List(employeeId As Integer?, status As AssetRecoveryStatus?) As IReadOnlyList(Of AssetRecoveryItem) Implements IAssetRecoveryRepository.List
        Dim items As New List(Of AssetRecoveryItem)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_AssetRecovery_List")
                cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = StoredProcedure.DbValue(employeeId)
                cmd.Parameters.Add("@StatusID", SqlDbType.TinyInt).Value = If(status.HasValue, CObj(CByte(status.Value)), DBNull.Value)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim reasonAt As Integer = reader.GetOrdinal("Reason")
                        Dim runAt As Integer = reader.GetOrdinal("AppliedRunID")
                        items.Add(New AssetRecoveryItem(reader.GetInt32(reader.GetOrdinal("RecoveryID")), reader.GetInt32(reader.GetOrdinal("AssetID")),
                                                        reader.GetString(reader.GetOrdinal("TagNo")), reader.GetString(reader.GetOrdinal("TypeName")),
                                                        reader.GetInt32(reader.GetOrdinal("EmployeeID")), reader.GetString(reader.GetOrdinal("EmpCode")),
                                                        reader.GetString(reader.GetOrdinal("FullName")), reader.GetDateTime(reader.GetOrdinal("BasisDate")),
                                                        reader.GetDecimal(reader.GetOrdinal("DepreciatedValue")), reader.GetDecimal(reader.GetOrdinal("Amount")),
                                                        If(reader.IsDBNull(reasonAt), String.Empty, reader.GetString(reasonAt)),
                                                        CType(CInt(reader.GetByte(reader.GetOrdinal("StatusID"))), AssetRecoveryStatus),
                                                        If(reader.IsDBNull(runAt), CType(Nothing, Integer?), reader.GetInt32(runAt))))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function
End Class
