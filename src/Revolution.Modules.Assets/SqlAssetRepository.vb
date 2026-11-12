Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Asset storage through ast.usp_AssetType_*, usp_Asset_* (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlAssetRepository
    Implements IAssetRepository

    Private Const ValuePrecision As Byte = 12
    Private Const ValueScale As Byte = 2

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function ListTypes(includeInactive As Boolean) As IReadOnlyList(Of AssetTypeItem) Implements IAssetRepository.ListTypes
        Dim items As New List(Of AssetTypeItem)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_AssetType_List")
                cmd.Parameters.Add("@IncludeInactive", SqlDbType.Bit).Value = includeInactive
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New AssetTypeItem(reader.GetInt32(reader.GetOrdinal("AssetTypeID")), reader.GetString(reader.GetOrdinal("TypeName")),
                                                    reader.GetInt32(reader.GetOrdinal("UsefulLifeMonths")), reader.GetBoolean(reader.GetOrdinal("IsActive"))))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Function SaveType(assetType As AssetTypeWrite, userId As Integer?) As Integer Implements IAssetRepository.SaveType
        If assetType Is Nothing Then Throw New ArgumentNullException(NameOf(assetType))
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_AssetType_Save")
                    cmd.Parameters.Add("@AssetTypeID", SqlDbType.Int).Value = StoredProcedure.DbValue(assetType.AssetTypeId)
                    cmd.Parameters.Add("@TypeName", SqlDbType.NVarChar, 60).Value = assetType.Name
                    cmd.Parameters.Add("@UsefulLifeMonths", SqlDbType.Int).Value = assetType.UsefulLifeMonths
                    cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = assetType.IsActive
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    Return Convert.ToInt32(cmd.ExecuteScalar(), Globalization.CultureInfo.InvariantCulture)
                End Using
            End Using
        Catch ex As SqlException When AssetsErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New AssetsRuleException(AssetsErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
    End Function

    Public Function ListAssets(status As AssetStatus?, employeeId As Integer?, heldOnly As Boolean) As IReadOnlyList(Of AssetItem) Implements IAssetRepository.ListAssets
        Dim items As New List(Of AssetItem)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_Asset_List")
                cmd.Parameters.Add("@StatusID", SqlDbType.TinyInt).Value = If(status.HasValue, CObj(CByte(status.Value)), DBNull.Value)
                cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = StoredProcedure.DbValue(employeeId)
                cmd.Parameters.Add("@HeldOnly", SqlDbType.Bit).Value = heldOnly
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New AssetItem(reader.GetInt32(reader.GetOrdinal("AssetID")), reader.GetInt32(reader.GetOrdinal("AssetTypeID")),
                                                reader.GetString(reader.GetOrdinal("TypeName")), reader.GetString(reader.GetOrdinal("TagNo")),
                                                reader.GetString(reader.GetOrdinal("Description")), NullableText(reader, "SerialNo"),
                                                reader.GetDateTime(reader.GetOrdinal("PurchaseDate")), reader.GetDecimal(reader.GetOrdinal("PurchaseValue")),
                                                CType(CInt(reader.GetByte(reader.GetOrdinal("StatusID"))), AssetStatus),
                                                NullableInt(reader, "HolderEmployeeID"), NullableText(reader, "HolderCode"), NullableText(reader, "HolderName"),
                                                NullableDate(reader, "IssueDate"), NullableDate(reader, "ExpectedReturnDate")))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Function AddAsset(asset As AssetWrite, userId As Integer?) As Integer Implements IAssetRepository.AddAsset
        If asset Is Nothing Then Throw New ArgumentNullException(NameOf(asset))
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_Asset_Add")
                    cmd.Parameters.Add("@AssetTypeID", SqlDbType.Int).Value = asset.AssetTypeId
                    cmd.Parameters.Add("@TagNo", SqlDbType.NVarChar, 30).Value = asset.TagNo
                    cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 200).Value = asset.Description
                    cmd.Parameters.Add("@SerialNo", SqlDbType.NVarChar, 60).Value = If(String.IsNullOrEmpty(asset.SerialNo), CObj(DBNull.Value), asset.SerialNo)
                    cmd.Parameters.Add("@PurchaseDate", SqlDbType.Date).Value = asset.PurchaseDate.Date
                    cmd.Parameters.Add(New SqlParameter("@PurchaseValue", SqlDbType.Decimal) With {.Precision = ValuePrecision, .Scale = ValueScale, .Value = asset.PurchaseValue})
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    Return Convert.ToInt32(cmd.ExecuteScalar(), Globalization.CultureInfo.InvariantCulture)
                End Using
            End Using
        Catch ex As SqlException When AssetsErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New AssetsRuleException(AssetsErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
    End Function

    Public Sub IssueAsset(issue As AssetIssueWrite, userId As Integer?) Implements IAssetRepository.IssueAsset
        If issue Is Nothing Then Throw New ArgumentNullException(NameOf(issue))
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_Asset_Issue")
                    cmd.Parameters.Add("@AssetID", SqlDbType.Int).Value = issue.AssetId
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = issue.EmployeeId
                    cmd.Parameters.Add("@IssueDate", SqlDbType.Date).Value = issue.IssueDate.Date
                    cmd.Parameters.Add("@IssueCondition", SqlDbType.NVarChar, 100).Value = If(String.IsNullOrEmpty(issue.Condition), CObj(DBNull.Value), issue.Condition)
                    cmd.Parameters.Add("@ExpectedReturnDate", SqlDbType.Date).Value = If(issue.ExpectedReturnDate.HasValue, CObj(issue.ExpectedReturnDate.Value.Date), DBNull.Value)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When AssetsErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New AssetsRuleException(AssetsErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
    End Sub

    Public Sub ReturnAsset(back As AssetReturnWrite, userId As Integer?) Implements IAssetRepository.ReturnAsset
        If back Is Nothing Then Throw New ArgumentNullException(NameOf(back))
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_Asset_Return")
                    cmd.Parameters.Add("@AssetID", SqlDbType.Int).Value = back.AssetId
                    cmd.Parameters.Add("@ReturnDate", SqlDbType.Date).Value = back.ReturnDate.Date
                    cmd.Parameters.Add("@ReturnCondition", SqlDbType.NVarChar, 100).Value = If(String.IsNullOrEmpty(back.Condition), CObj(DBNull.Value), back.Condition)
                    cmd.Parameters.Add("@ReturnStatusID", SqlDbType.TinyInt).Value = CByte(back.ReturnStatus)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When AssetsErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New AssetsRuleException(AssetsErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
    End Sub

    Public Sub SetStatus(assetId As Integer, status As AssetStatus, userId As Integer?) Implements IAssetRepository.SetStatus
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_Asset_SetStatus")
                    cmd.Parameters.Add("@AssetID", SqlDbType.Int).Value = assetId
                    cmd.Parameters.Add("@StatusID", SqlDbType.TinyInt).Value = CByte(status)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When AssetsErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New AssetsRuleException(AssetsErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
    End Sub

    Public Function History(assetId As Integer) As IReadOnlyList(Of AssetIssueItem) Implements IAssetRepository.History
        Dim items As New List(Of AssetIssueItem)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_Asset_History")
                cmd.Parameters.Add("@AssetID", SqlDbType.Int).Value = assetId
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New AssetIssueItem(reader.GetInt32(reader.GetOrdinal("IssueID")), reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                                     reader.GetString(reader.GetOrdinal("EmpCode")), reader.GetString(reader.GetOrdinal("FullName")),
                                                     reader.GetDateTime(reader.GetOrdinal("IssueDate")), NullableText(reader, "IssueCondition"),
                                                     NullableDate(reader, "ExpectedReturnDate"), NullableDate(reader, "ReturnDate"), NullableText(reader, "ReturnCondition")))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Private Shared Function NullableText(reader As SqlDataReader, column As String) As String
        Dim at As Integer = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(at), String.Empty, reader.GetString(at))
    End Function

    Private Shared Function NullableInt(reader As SqlDataReader, column As String) As Integer?
        Dim at As Integer = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(at), CType(Nothing, Integer?), reader.GetInt32(at))
    End Function

    Private Shared Function NullableDate(reader As SqlDataReader, column As String) As Date?
        Dim at As Integer = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(at), CType(Nothing, Date?), reader.GetDateTime(at))
    End Function
End Class
