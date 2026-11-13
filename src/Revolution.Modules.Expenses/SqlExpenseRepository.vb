Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Expense storage through ast.usp_ExpenseCategory_* and usp_ExpenseClaim_* (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlExpenseRepository
    Implements IExpenseRepository

    Private Const MoneyPrecision As Byte = 12
    Private Const MoneyScale As Byte = 2

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function ListCategories(includeInactive As Boolean) As IReadOnlyList(Of ExpenseCategoryItem) Implements IExpenseRepository.ListCategories
        Dim items As New List(Of ExpenseCategoryItem)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_ExpenseCategory_List")
                cmd.Parameters.Add("@IncludeInactive", SqlDbType.Bit).Value = includeInactive
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New ExpenseCategoryItem(reader.GetInt32(reader.GetOrdinal("CategoryID")), reader.GetString(reader.GetOrdinal("CategoryName")),
                                                          NullableMoney(reader, "PerClaimLimit"), NullableMoney(reader, "PerMonthLimit"), reader.GetBoolean(reader.GetOrdinal("IsActive"))))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Function SaveCategory(category As ExpenseCategoryWrite, userId As Integer?) As Integer Implements IExpenseRepository.SaveCategory
        If category Is Nothing Then Throw New ArgumentNullException(NameOf(category))
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_ExpenseCategory_Save")
                    cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = StoredProcedure.DbValue(category.CategoryId)
                    cmd.Parameters.Add("@CategoryName", SqlDbType.NVarChar, 60).Value = category.Name
                    cmd.Parameters.Add(Money("@PerClaimLimit", category.PerClaimLimit))
                    cmd.Parameters.Add(Money("@PerMonthLimit", category.PerMonthLimit))
                    cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = category.IsActive
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    Return Convert.ToInt32(cmd.ExecuteScalar(), Globalization.CultureInfo.InvariantCulture)
                End Using
            End Using
        Catch ex As SqlException When ExpensesErrorMap.Translate(ex.Number) IsNot Nothing
            Throw Wrap(ex)
        End Try
    End Function

    Public Function AddClaim(claim As ExpenseClaimWrite, userId As Integer?) As Integer Implements IExpenseRepository.AddClaim
        If claim Is Nothing Then Throw New ArgumentNullException(NameOf(claim))
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_ExpenseClaim_Add")
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = claim.EmployeeId
                    cmd.Parameters.Add("@CategoryID", SqlDbType.Int).Value = claim.CategoryId
                    cmd.Parameters.Add("@ExpenseDate", SqlDbType.Date).Value = claim.ExpenseDate.Date
                    cmd.Parameters.Add(Money("@Amount", claim.Amount))
                    cmd.Parameters.Add("@BillNo", SqlDbType.NVarChar, 40).Value = Text(claim.BillNo)
                    cmd.Parameters.Add("@BillDate", SqlDbType.Date).Value = If(claim.BillDate.HasValue, CObj(claim.BillDate.Value.Date), DBNull.Value)
                    cmd.Parameters.Add("@Vendor", SqlDbType.NVarChar, 80).Value = Text(claim.Vendor)
                    cmd.Parameters.Add("@Note", SqlDbType.NVarChar, 200).Value = Text(claim.Note)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    Return Convert.ToInt32(cmd.ExecuteScalar(), Globalization.CultureInfo.InvariantCulture)
                End Using
            End Using
        Catch ex As SqlException When ExpensesErrorMap.Translate(ex.Number) IsNot Nothing
            Throw Wrap(ex)
        End Try
    End Function

    Public Function ListClaims(employeeId As Integer?, status As ExpenseClaimStatus?, fromDate As Date?, toDate As Date?) As IReadOnlyList(Of ExpenseClaimItem) Implements IExpenseRepository.ListClaims
        Dim items As New List(Of ExpenseClaimItem)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "ast.usp_ExpenseClaim_List")
                cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = StoredProcedure.DbValue(employeeId)
                cmd.Parameters.Add("@StatusID", SqlDbType.TinyInt).Value = If(status.HasValue, CObj(CByte(status.Value)), DBNull.Value)
                cmd.Parameters.Add("@FromDate", SqlDbType.Date).Value = If(fromDate.HasValue, CObj(fromDate.Value.Date), DBNull.Value)
                cmd.Parameters.Add("@ToDate", SqlDbType.Date).Value = If(toDate.HasValue, CObj(toDate.Value.Date), DBNull.Value)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim modeAt As Integer = reader.GetOrdinal("PaymentMode")
                        items.Add(New ExpenseClaimItem(reader.GetInt32(reader.GetOrdinal("ClaimID")), reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                                       reader.GetString(reader.GetOrdinal("EmpCode")), reader.GetString(reader.GetOrdinal("FullName")),
                                                       reader.GetInt32(reader.GetOrdinal("CategoryID")), reader.GetString(reader.GetOrdinal("CategoryName")),
                                                       reader.GetDateTime(reader.GetOrdinal("ExpenseDate")), reader.GetDecimal(reader.GetOrdinal("Amount")),
                                                       NullableText(reader, "BillNo"), NullableDate(reader, "BillDate"), NullableText(reader, "Vendor"), NullableText(reader, "Note"),
                                                       CType(CInt(reader.GetByte(reader.GetOrdinal("StatusID"))), ExpenseClaimStatus), NullableText(reader, "DecisionReason"),
                                                       NullableDate(reader, "PaidDate"),
                                                       If(reader.IsDBNull(modeAt), CType(Nothing, ExpensePaymentMode?), CType(CInt(reader.GetByte(modeAt)), ExpensePaymentMode)),
                                                       NullableText(reader, "PaymentRef")))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Sub Decide(claimId As Integer, approve As Boolean, reason As String, userId As Integer?) Implements IExpenseRepository.Decide
        Execute("ast.usp_ExpenseClaim_Decide",
                Sub(cmd)
                    cmd.Parameters.Add("@ClaimID", SqlDbType.Int).Value = claimId
                    cmd.Parameters.Add("@Approve", SqlDbType.Bit).Value = approve
                    cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, 200).Value = Text(reason)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Public Sub Pay(claimId As Integer, paidDate As Date, mode As ExpensePaymentMode, reference As String, userId As Integer?) Implements IExpenseRepository.Pay
        Execute("ast.usp_ExpenseClaim_Pay",
                Sub(cmd)
                    cmd.Parameters.Add("@ClaimID", SqlDbType.Int).Value = claimId
                    cmd.Parameters.Add("@PaidDate", SqlDbType.Date).Value = paidDate.Date
                    cmd.Parameters.Add("@PaymentMode", SqlDbType.TinyInt).Value = CByte(mode)
                    cmd.Parameters.Add("@PaymentRef", SqlDbType.NVarChar, 60).Value = Text(reference)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Public Sub Cancel(claimId As Integer, reason As String, userId As Integer?) Implements IExpenseRepository.Cancel
        Execute("ast.usp_ExpenseClaim_Cancel",
                Sub(cmd)
                    cmd.Parameters.Add("@ClaimID", SqlDbType.Int).Value = claimId
                    cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, 200).Value = Text(reason)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Private Sub Execute(procedure As String, bind As Action(Of SqlCommand))
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, procedure)
                    bind(cmd)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When ExpensesErrorMap.Translate(ex.Number) IsNot Nothing
            Throw Wrap(ex)
        End Try
    End Sub

    Private Shared Function Wrap(ex As SqlException) As ExpensesRuleException
        Dim message As String = If(ExpensesErrorMap.CarriesLimit(ex.Number), ex.Message, ExpensesErrorMap.Translate(ex.Number))
        Return New ExpensesRuleException(message, ex.Number, ex)
    End Function

    Private Shared Function Money(name As String, value As Decimal?) As SqlParameter
        Return New SqlParameter(name, SqlDbType.Decimal) With {.Precision = MoneyPrecision, .Scale = MoneyScale, .Value = If(value.HasValue, CObj(value.Value), DBNull.Value)}
    End Function

    Private Shared Function Text(value As String) As Object
        Return If(String.IsNullOrEmpty(value), CObj(DBNull.Value), value)
    End Function

    Private Shared Function NullableMoney(reader As SqlDataReader, column As String) As Decimal?
        Dim at As Integer = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(at), CType(Nothing, Decimal?), reader.GetDecimal(at))
    End Function

    Private Shared Function NullableText(reader As SqlDataReader, column As String) As String
        Dim at As Integer = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(at), String.Empty, reader.GetString(at))
    End Function

    Private Shared Function NullableDate(reader As SqlDataReader, column As String) As Date?
        Dim at As Integer = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(at), CType(Nothing, Date?), reader.GetDateTime(at))
    End Function
End Class
