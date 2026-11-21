Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Core
Imports Revolution.Data

''' <summary>
''' Monthly Pack storage through pay.usp_Pack_Figures, pay.usp_Pack_LwfStates, pay.usp_Filing_List, pay.usp_Filing_Record and stat.usp_DueDate_Resolve (ADR 0001, ADR 0027).
''' Tenant context comes from IConnectionFactory. Errors 53650 to 53699 become PackRuleException with the procedure's own message.
''' </summary>
Public NotInheritable Class SqlMonthlyPackRepository
    Implements IMonthlyPackRepository

    Private Const FirstPackError As Integer = 53650
    Private Const LastPackError As Integer = 53699
    Private Const MaxReferenceLength As Integer = 40

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function GetFigures(periodStart As Date) As PackFigures Implements IMonthlyPackRepository.GetFigures
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Pack_Figures")
                    cmd.Parameters.Add("@PeriodStart", SqlDbType.Date).Value = periodStart.Date
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        Dim company As String = String.Empty
                        If reader.Read() Then company = reader.GetString(0)
                        Dim runId As Integer? = Nothing
                        Dim status As PayrollRunStatus? = Nothing
                        Dim periodTo As Date? = Nothing
                        reader.NextResult()
                        If reader.Read() Then
                            runId = reader.GetInt32(reader.GetOrdinal("RunID"))
                            status = CType(CInt(reader.GetByte(reader.GetOrdinal("StatusID"))), PayrollRunStatus)
                            periodTo = reader.GetDateTime(reader.GetOrdinal("PeriodTo"))
                        End If
                        reader.NextResult()
                        reader.Read()
                        Dim epfEmployee = reader.GetDecimal(reader.GetOrdinal("EpfEmployee"))
                        Dim epfEmployerEpf = reader.GetDecimal(reader.GetOrdinal("EpfEmployerEpf"))
                        Dim epfEmployerEps = reader.GetDecimal(reader.GetOrdinal("EpfEmployerEps"))
                        Dim edli = reader.GetDecimal(reader.GetOrdinal("Edli"))
                        Dim epfAdmin = reader.GetDecimal(reader.GetOrdinal("EpfAdmin"))
                        Dim epfCount = reader.GetInt32(reader.GetOrdinal("EpfCount"))
                        Dim esiEmployee = reader.GetDecimal(reader.GetOrdinal("EsiEmployee"))
                        Dim esiEmployer = reader.GetDecimal(reader.GetOrdinal("EsiEmployer"))
                        Dim esiCount = reader.GetInt32(reader.GetOrdinal("EsiCount"))
                        Dim states As New List(Of PackStateAmount)()
                        reader.NextResult()
                        While reader.Read()
                            states.Add(New PackStateAmount(CInt(reader.GetByte(reader.GetOrdinal("StateID"))), reader.GetString(reader.GetOrdinal("StateName")),
                                                           reader.GetDecimal(reader.GetOrdinal("Amount")), reader.GetInt32(reader.GetOrdinal("EmployeeCount"))))
                        End While
                        Return New PackFigures(company, runId, status, periodTo, epfEmployee, epfEmployerEpf, epfEmployerEps, edli, epfAdmin, epfCount,
                                               esiEmployee, esiEmployer, esiCount, states)
                    End Using
                End Using
            End Using
        Catch ex As SqlException When IsPackError(ex)
            Throw New PackRuleException(ex.Message, ex)
        End Try
    End Function

    Public Function LwfStates(periodStart As Date) As IReadOnlyList(Of PackStateAmount) Implements IMonthlyPackRepository.LwfStates
        Dim list As New List(Of PackStateAmount)()
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Pack_LwfStates")
                    cmd.Parameters.Add("@PeriodStart", SqlDbType.Date).Value = periodStart.Date
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            list.Add(New PackStateAmount(CInt(reader.GetByte(reader.GetOrdinal("StateID"))), reader.GetString(reader.GetOrdinal("StateName")), 0D, 0))
                        End While
                    End Using
                End Using
            End Using
        Catch ex As SqlException When IsPackError(ex)
            Throw New PackRuleException(ex.Message, ex)
        End Try
        Return list
    End Function

    Public Function ListFilings(periodStart As Date) As IReadOnlyList(Of PackFiling) Implements IMonthlyPackRepository.ListFilings
        Dim list As New List(Of PackFiling)()
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Filing_List")
                    cmd.Parameters.Add("@PeriodStart", SqlDbType.Date).Value = periodStart.Date
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim stateOrdinal = reader.GetOrdinal("StateID")
                            Dim filedOrdinal = reader.GetOrdinal("FiledOn")
                            Dim paidOrdinal = reader.GetOrdinal("PaidOn")
                            Dim refOrdinal = reader.GetOrdinal("ReferenceNo")
                            list.Add(New PackFiling(CType(CInt(reader.GetByte(reader.GetOrdinal("Kind"))), ComplianceKind),
                                                    If(reader.IsDBNull(stateOrdinal), CType(Nothing, Integer?), CInt(reader.GetByte(stateOrdinal))),
                                                    CType(CInt(reader.GetByte(reader.GetOrdinal("Status"))), PackStatus),
                                                    reader.GetDecimal(reader.GetOrdinal("AmountPayable")),
                                                    reader.GetInt32(reader.GetOrdinal("EmployeeCount")),
                                                    If(reader.IsDBNull(filedOrdinal), CType(Nothing, Date?), reader.GetDateTime(filedOrdinal)),
                                                    If(reader.IsDBNull(paidOrdinal), CType(Nothing, Date?), reader.GetDateTime(paidOrdinal)),
                                                    If(reader.IsDBNull(refOrdinal), String.Empty, reader.GetString(refOrdinal))))
                        End While
                    End Using
                End Using
            End Using
        Catch ex As SqlException When IsPackError(ex)
            Throw New PackRuleException(ex.Message, ex)
        End Try
        Return list
    End Function

    Public Sub RecordFiling(periodStart As Date, kind As ComplianceKind, stateId As Integer?, status As PackStatus, amountPayable As Decimal?, employeeCount As Integer?,
                            filedOn As Date?, paidOn As Date?, referenceNo As String, userId As Integer?) Implements IMonthlyPackRepository.RecordFiling
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Filing_Record")
                    cmd.Parameters.Add("@PeriodStart", SqlDbType.Date).Value = periodStart.Date
                    cmd.Parameters.Add("@Kind", SqlDbType.TinyInt).Value = CByte(CInt(kind))
                    cmd.Parameters.Add("@StateID", SqlDbType.TinyInt).Value = If(stateId.HasValue, CObj(CByte(stateId.Value)), DBNull.Value)
                    cmd.Parameters.Add("@Status", SqlDbType.TinyInt).Value = CByte(CInt(status))
                    Dim amount = cmd.Parameters.Add("@AmountPayable", SqlDbType.Decimal)
                    amount.Precision = 14
                    amount.Scale = 2
                    amount.Value = StoredProcedure.DbValue(amountPayable)
                    cmd.Parameters.Add("@EmployeeCount", SqlDbType.Int).Value = StoredProcedure.DbValue(employeeCount)
                    cmd.Parameters.Add("@FiledOn", SqlDbType.Date).Value = StoredProcedure.DbValue(filedOn)
                    cmd.Parameters.Add("@PaidOn", SqlDbType.Date).Value = StoredProcedure.DbValue(paidOn)
                    cmd.Parameters.Add("@ReferenceNo", SqlDbType.NVarChar, MaxReferenceLength).Value = If(String.IsNullOrEmpty(referenceNo), DBNull.Value, CObj(referenceNo))
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When IsPackError(ex)
            Throw New PackRuleException(ex.Message, ex)
        End Try
    End Sub

    Public Function ResolveDue(kind As ComplianceKind, stateId As Integer?, periodStart As Date) As PackDue Implements IMonthlyPackRepository.ResolveDue
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "stat.usp_DueDate_Resolve")
                    cmd.Parameters.Add("@Kind", SqlDbType.TinyInt).Value = CByte(CInt(kind))
                    cmd.Parameters.Add("@StateID", SqlDbType.TinyInt).Value = If(stateId.HasValue, CObj(CByte(stateId.Value)), DBNull.Value)
                    cmd.Parameters.Add("@PeriodStart", SqlDbType.Date).Value = periodStart.Date
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        If Not reader.Read() Then Return New PackDue(Nothing, False, False)
                        Dim dueOrdinal = reader.GetOrdinal("DueDate")
                        Return New PackDue(If(reader.IsDBNull(dueOrdinal), CType(Nothing, Date?), reader.GetDateTime(dueOrdinal)),
                                           reader.GetBoolean(reader.GetOrdinal("HasRule")), reader.GetBoolean(reader.GetOrdinal("IsVerified")))
                    End Using
                End Using
            End Using
        Catch ex As SqlException When IsPackError(ex)
            Throw New PackRuleException(ex.Message, ex)
        End Try
    End Function

    Private Shared Function IsPackError(ex As SqlException) As Boolean
        Return ex.Number >= FirstPackError AndAlso ex.Number <= LastPackError
    End Function
End Class
