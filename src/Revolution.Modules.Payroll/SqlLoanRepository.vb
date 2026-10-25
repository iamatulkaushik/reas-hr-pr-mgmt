Imports System.Data
Imports System.Globalization
Imports System.Text.Json
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Loan storage through pay.usp_LoanType_*, usp_Loan_* and usp_LoanInstallment_List (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlLoanRepository
    Implements ILoanRepository

    Private Const MaxNameLength As Integer = 100

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function ListTypes() As IReadOnlyList(Of LoanTypeItem) Implements ILoanRepository.ListTypes
        Dim items As New List(Of LoanTypeItem)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_LoanType_List")
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New LoanTypeItem(reader.GetInt32(reader.GetOrdinal("LoanTypeID")),
                                                   reader.GetString(reader.GetOrdinal("Name")),
                                                   CType(CInt(reader.GetByte(reader.GetOrdinal("InterestMethodID"))), InterestMethod),
                                                   reader.GetDecimal(reader.GetOrdinal("AnnualInterestPct")),
                                                   CInt(reader.GetInt16(reader.GetOrdinal("MaxInstallments"))),
                                                   reader.GetBoolean(reader.GetOrdinal("IsActive"))))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Function SaveType(loanType As LoanTypeItem) As Integer Implements ILoanRepository.SaveType
        Return ExecuteScalarInt("pay.usp_LoanType_Save",
                                Sub(cmd)
                                    cmd.Parameters.Add("@LoanTypeID", SqlDbType.Int).Value = StoredProcedure.DbValue(loanType.LoanTypeId)
                                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, MaxNameLength).Value = loanType.Name
                                    cmd.Parameters.Add("@InterestMethodID", SqlDbType.TinyInt).Value = CByte(loanType.Method)
                                    AddDecimal(cmd, "@AnnualInterestPct", 7, 4, loanType.AnnualInterestPct)
                                    cmd.Parameters.Add("@MaxInstallments", SqlDbType.SmallInt).Value = CShort(loanType.MaxInstallments)
                                    cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = loanType.IsActive
                                End Sub)
    End Function

    Public Function CreateLoan(employeeId As Integer, loanTypeId As Integer, sanctionDate As Date, principal As Decimal, method As InterestMethod,
                               annualInterestPct As Decimal, schedule As IReadOnlyList(Of LoanInstallmentPlan), userId As Integer?) As Integer Implements ILoanRepository.CreateLoan
        Dim json As String = JsonSerializer.Serialize(schedule.Select(Function(p) New With {.No = p.InstallmentNo, .DueMonthKey = p.DueMonthKey, .Principal = p.PrincipalPart,
                                                                                            .Interest = p.InterestPart, .Amount = p.Amount}).ToList())
        Return ExecuteScalarInt("pay.usp_Loan_Create",
                                Sub(cmd)
                                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                                    cmd.Parameters.Add("@LoanTypeID", SqlDbType.Int).Value = loanTypeId
                                    cmd.Parameters.Add("@SanctionDate", SqlDbType.Date).Value = sanctionDate
                                    AddDecimal(cmd, "@Principal", 14, 2, principal)
                                    cmd.Parameters.Add("@InterestMethodID", SqlDbType.TinyInt).Value = CByte(method)
                                    AddDecimal(cmd, "@AnnualInterestPct", 7, 4, annualInterestPct)
                                    cmd.Parameters.Add("@InstallmentsJson", SqlDbType.NVarChar, -1).Value = json
                                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                                End Sub)
    End Function

    Public Sub CancelLoan(loanId As Integer) Implements ILoanRepository.CancelLoan
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Loan_Cancel")
                    cmd.Parameters.Add("@LoanID", SqlDbType.Int).Value = loanId
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When PayrollErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(PayrollErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
    End Sub

    Public Function ListLoans(employeeId As Integer) As IReadOnlyList(Of LoanSummary) Implements ILoanRepository.ListLoans
        Dim items As New List(Of LoanSummary)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Loan_ListByEmployee")
                cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New LoanSummary(reader.GetInt32(reader.GetOrdinal("LoanID")),
                                                  reader.GetInt32(reader.GetOrdinal("LoanTypeID")),
                                                  reader.GetDateTime(reader.GetOrdinal("SanctionDate")),
                                                  reader.GetDecimal(reader.GetOrdinal("Principal")),
                                                  CType(CInt(reader.GetByte(reader.GetOrdinal("InterestMethodID"))), InterestMethod),
                                                  reader.GetDecimal(reader.GetOrdinal("AnnualInterestPct")),
                                                  CInt(reader.GetInt16(reader.GetOrdinal("InstallmentCount"))),
                                                  CType(CInt(reader.GetByte(reader.GetOrdinal("StatusID"))), LoanStatus),
                                                  reader.GetDecimal(reader.GetOrdinal("Outstanding"))))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Function ListInstallments(loanId As Integer) As IReadOnlyList(Of LoanInstallmentView) Implements ILoanRepository.ListInstallments
        Dim items As New List(Of LoanInstallmentView)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_LoanInstallment_List")
                cmd.Parameters.Add("@LoanID", SqlDbType.Int).Value = loanId
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New LoanInstallmentView(CInt(reader.GetInt16(reader.GetOrdinal("InstallmentNo"))),
                                                          reader.GetInt32(reader.GetOrdinal("DueMonthKey")),
                                                          reader.GetDecimal(reader.GetOrdinal("PrincipalPart")),
                                                          reader.GetDecimal(reader.GetOrdinal("InterestPart")),
                                                          reader.GetDecimal(reader.GetOrdinal("Amount")),
                                                          reader.GetDecimal(reader.GetOrdinal("Recovered"))))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    ''' <summary>A decimal parameter with its column precision and scale, so SqlClient does not round the value to a whole number.</summary>
    Private Shared Sub AddDecimal(cmd As SqlCommand, name As String, precision As Byte, scale As Byte, value As Decimal)
        Dim parameter As SqlParameter = cmd.Parameters.Add(name, SqlDbType.Decimal)
        parameter.Precision = precision
        parameter.Scale = scale
        parameter.Value = value
    End Sub

    Private Function ExecuteScalarInt(procedure As String, bind As Action(Of SqlCommand)) As Integer
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, procedure)
                    bind(cmd)
                    Return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture)
                End Using
            End Using
        Catch ex As SqlException When PayrollErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(PayrollErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
    End Function
End Class
