Imports System.Data
Imports System.Globalization
Imports Microsoft.Data.SqlClient
Imports Revolution.Data
Imports Revolution.Modules.Payroll

''' <summary>Arrears storage through pay.usp_ArrearsClaim_* and usp_ArrearsReport (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlArrearsRepository
    Implements IArrearsRepository

    Private Const MaxReasonLength As Integer = 200

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function Add(claim As NewArrearsClaim) As Integer Implements IArrearsRepository.Add
        If claim Is Nothing Then Throw New ArgumentNullException(NameOf(claim))
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_ArrearsClaim_Add")
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = claim.EmployeeId
                    cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, MaxReasonLength).Value = claim.Reason
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(claim.UserId)
                    cmd.Parameters.Add("@MonthsJson", SqlDbType.NVarChar, -1).Value = ArrearsMonthsJson.Build(claim.Months)
                    Return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture)
                End Using
            End Using
        Catch ex As SqlException When ArrearsErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(ArrearsErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
    End Function

    Public Function List(filter As ArrearsFilter) As IReadOnlyList(Of ArrearsClaimRow) Implements IArrearsRepository.List
        If filter Is Nothing Then Throw New ArgumentNullException(NameOf(filter))
        Dim rows As New List(Of ArrearsClaimRow)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_ArrearsClaim_List")
                cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = StoredProcedure.DbValue(filter.EmployeeId)
                cmd.Parameters.Add("@FromMonth", SqlDbType.Date).Value = DateValue(filter.FromMonth)
                cmd.Parameters.Add("@ToMonth", SqlDbType.Date).Value = DateValue(filter.ToMonth)
                cmd.Parameters.Add("@IncludeCancelled", SqlDbType.Bit).Value = filter.IncludeCancelled
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        rows.Add(ReadClaim(reader))
                    End While
                End Using
            End Using
        End Using
        Return rows
    End Function

    Public Function [Get](claimId As Integer) As ArrearsClaimDetail Implements IArrearsRepository.Get
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_ArrearsClaim_Get")
                    cmd.Parameters.Add("@ClaimID", SqlDbType.Int).Value = claimId
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        If Not reader.Read() Then Throw New PayrollRuleException(ArrearsErrorMap.Translate(53103, String.Empty))
                        Dim claim As ArrearsClaimRow = ReadClaim(reader)
                        Dim months As New List(Of ArrearsMonthRecord)()
                        If reader.NextResult() Then
                            While reader.Read()
                                months.Add(ReadMonth(reader))
                            End While
                        End If
                        Dim heads As New List(Of Tuple(Of Date, ArrearsHeadAmount))()
                        If reader.NextResult() Then
                            While reader.Read()
                                heads.Add(Tuple.Create(reader.GetDateTime(reader.GetOrdinal("WageMonth")),
                                                       New ArrearsHeadAmount(reader.GetInt32(reader.GetOrdinal("ComponentID")), reader.GetDecimal(reader.GetOrdinal("Amount")))))
                            End While
                        End If
                        Return New ArrearsClaimDetail(claim, months.Select(Function(m As ArrearsMonthRecord) WithHeads(m, heads)).ToList())
                    End Using
                End Using
            End Using
        Catch ex As SqlException When ArrearsErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(ArrearsErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
    End Function

    Public Sub Cancel(claimId As Integer, reason As String, userId As Integer?) Implements IArrearsRepository.Cancel
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_ArrearsClaim_Cancel")
                    cmd.Parameters.Add("@ClaimID", SqlDbType.Int).Value = claimId
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, MaxReasonLength).Value = reason
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When ArrearsErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(ArrearsErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
    End Sub

    Public Sub MarkPaid(runId As Integer, claimIds As IReadOnlyCollection(Of Integer)) Implements IArrearsRepository.MarkPaid
        If claimIds Is Nothing Then Throw New ArgumentNullException(NameOf(claimIds))
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_ArrearsClaim_Apply")
                    cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                    cmd.Parameters.Add("@ClaimIdsJson", SqlDbType.NVarChar, -1).Value = System.Text.Json.JsonSerializer.Serialize(claimIds)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When ArrearsErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(ArrearsErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
    End Sub

    Public Sub Release(runId As Integer) Implements IArrearsRepository.Release
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_ArrearsClaim_Release")
                    cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When ArrearsErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(ArrearsErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
    End Sub

    Public Function EcrLines(wageMonth As Date) As IReadOnlyList(Of ArrearsEcrLine) Implements IArrearsRepository.EcrLines
        Dim lines As New List(Of ArrearsEcrLine)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_ArrearsEcr_List")
                cmd.Parameters.Add("@WageMonth", SqlDbType.Date).Value = wageMonth.Date
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        lines.Add(New ArrearsEcrLine(reader.GetInt32(reader.GetOrdinal("EmployeeID")), reader.GetString(reader.GetOrdinal("EmpCode")),
                                                     reader.GetString(reader.GetOrdinal("FullName")), reader.GetDecimal(reader.GetOrdinal("EpfWages")),
                                                     reader.GetDecimal(reader.GetOrdinal("EpsWages")), reader.GetDecimal(reader.GetOrdinal("EdliWages")),
                                                     reader.GetDecimal(reader.GetOrdinal("EmployeeEpf")), reader.GetDecimal(reader.GetOrdinal("EmployerEps")),
                                                     reader.GetDecimal(reader.GetOrdinal("EmployerEpf"))))
                    End While
                End Using
            End Using
        End Using
        Return lines
    End Function

    Public Function Report(fromMonth As Date, toMonth As Date) As IReadOnlyList(Of ArrearsReportRow) Implements IArrearsRepository.Report
        Dim rows As New List(Of ArrearsReportRow)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_ArrearsReport")
                cmd.Parameters.Add("@FromMonth", SqlDbType.Date).Value = fromMonth.Date
                cmd.Parameters.Add("@ToMonth", SqlDbType.Date).Value = toMonth.Date
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        rows.Add(New ArrearsReportRow(reader.GetInt32(reader.GetOrdinal("ClaimID")), reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                                      reader.GetString(reader.GetOrdinal("EmpCode")), reader.GetString(reader.GetOrdinal("FullName")),
                                                      reader.GetDateTime(reader.GetOrdinal("WageMonth")), reader.GetDecimal(reader.GetOrdinal("GrossDifference")),
                                                      reader.GetDecimal(reader.GetOrdinal("EmployeeEpf")), reader.GetDecimal(reader.GetOrdinal("EsiEmployee")),
                                                      reader.GetDecimal(reader.GetOrdinal("EmployerTotal"))))
                    End While
                End Using
            End Using
        End Using
        Return rows
    End Function

    Private Shared Function DateValue(value As Date?) As Object
        Return If(value.HasValue, CType(value.Value.Date, Object), DBNull.Value)
    End Function

    Private Shared Function ReadClaim(reader As SqlDataReader) As ArrearsClaimRow
        Return New ArrearsClaimRow(reader.GetInt32(reader.GetOrdinal("ClaimID")), reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                   reader.GetString(reader.GetOrdinal("EmpCode")), reader.GetString(reader.GetOrdinal("FullName")),
                                   reader.GetDateTime(reader.GetOrdinal("FromMonth")), reader.GetDateTime(reader.GetOrdinal("ToMonth")),
                                   reader.GetString(reader.GetOrdinal("Reason")), CType(CInt(reader.GetByte(reader.GetOrdinal("StatusID"))), ArrearsClaimStatus),
                                   reader.GetDecimal(reader.GetOrdinal("GrossTotal")), reader.GetDecimal(reader.GetOrdinal("EmployeeDeductions")),
                                   reader.GetDecimal(reader.GetOrdinal("EmployerContributions")), reader.GetDecimal(reader.GetOrdinal("NetBeforeTax")),
                                   reader.GetDateTime(reader.GetOrdinal("CreatedUtc")),
                                   If(reader.IsDBNull(reader.GetOrdinal("AppliedRunID")), CType(Nothing, Integer?), reader.GetInt32(reader.GetOrdinal("AppliedRunID"))))
    End Function

    Private Shared Function WithHeads(m As ArrearsMonthRecord, heads As List(Of Tuple(Of Date, ArrearsHeadAmount))) As ArrearsMonthRecord
        Dim mine As List(Of ArrearsHeadAmount) = heads.Where(Function(h As Tuple(Of Date, ArrearsHeadAmount)) h.Item1 = m.WageMonth).Select(Function(h As Tuple(Of Date, ArrearsHeadAmount)) h.Item2).ToList()
        Return New ArrearsMonthRecord(m.WageMonth, m.PaidGross, m.DueGross, m.GrossDifference, m.EpfWages, m.EpsWages, m.EdliWages, m.EmployeeEpf, m.EmployerEpf,
                                      m.EmployerEps, m.Edli, m.Admin, m.EsiEmployee, m.EsiEmployer, mine, m.OvertimeDifference, m.EsiWages)
    End Function

    Private Shared Function ReadMonth(reader As SqlDataReader) As ArrearsMonthRecord
        Return New ArrearsMonthRecord(reader.GetDateTime(reader.GetOrdinal("WageMonth")), reader.GetDecimal(reader.GetOrdinal("PaidGross")),
                                      reader.GetDecimal(reader.GetOrdinal("DueGross")), reader.GetDecimal(reader.GetOrdinal("GrossDifference")),
                                      reader.GetDecimal(reader.GetOrdinal("EpfWages")), reader.GetDecimal(reader.GetOrdinal("EpsWages")),
                                      reader.GetDecimal(reader.GetOrdinal("EdliWages")), reader.GetDecimal(reader.GetOrdinal("EmployeeEpf")),
                                      reader.GetDecimal(reader.GetOrdinal("EmployerEpf")), reader.GetDecimal(reader.GetOrdinal("EmployerEps")),
                                      reader.GetDecimal(reader.GetOrdinal("Edli")), reader.GetDecimal(reader.GetOrdinal("Admin")),
                                      reader.GetDecimal(reader.GetOrdinal("EsiEmployee")), reader.GetDecimal(reader.GetOrdinal("EsiEmployer")), Nothing,
                                      reader.GetDecimal(reader.GetOrdinal("OvertimeDifference")), reader.GetDecimal(reader.GetOrdinal("EsiWages")))
    End Function
End Class
