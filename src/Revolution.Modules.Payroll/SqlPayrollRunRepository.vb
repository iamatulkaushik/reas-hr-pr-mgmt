Imports System.Data
Imports System.Globalization
Imports System.Text.Json
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Payroll run storage through the pay.usp_Run*, usp_RunInput_List, usp_LoanInstallment_ListOpen and usp_Recovery_* procedures (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlPayrollRunRepository
    Implements IPayrollRunRepository

    Private Const MaxActionLength As Integer = 30
    Private Const MaxReasonLength As Integer = 500

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function CreateRun(period As RunPeriod, runNo As Integer, userId As Integer?) As Integer Implements IPayrollRunRepository.CreateRun
        Return ExecuteScalarInt("pay.usp_Run_Create",
                                Sub(cmd)
                                    cmd.Parameters.Add("@PeriodFrom", SqlDbType.Date).Value = period.PeriodFrom
                                    cmd.Parameters.Add("@PeriodTo", SqlDbType.Date).Value = period.PeriodTo
                                    cmd.Parameters.Add("@RunNo", SqlDbType.TinyInt).Value = CByte(runNo)
                                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                                End Sub)
    End Function

    Public Function GetRun(runId As Integer) As PayrollRunHeader Implements IPayrollRunRepository.GetRun
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Run_Get")
                cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    If Not reader.Read() Then Return Nothing
                    Return New PayrollRunHeader(reader.GetInt32(reader.GetOrdinal("RunID")),
                                                reader.GetDateTime(reader.GetOrdinal("PeriodFrom")),
                                                reader.GetDateTime(reader.GetOrdinal("PeriodTo")),
                                                CInt(reader.GetByte(reader.GetOrdinal("RunNo"))),
                                                CType(CInt(reader.GetByte(reader.GetOrdinal("StatusID"))), PayrollRunStatus),
                                                reader.GetInt32(reader.GetOrdinal("EmployeeCount")),
                                                reader.GetDecimal(reader.GetOrdinal("TotalGross")),
                                                reader.GetDecimal(reader.GetOrdinal("TotalDeductions")),
                                                reader.GetDecimal(reader.GetOrdinal("TotalNet")))
                End Using
            End Using
        End Using
    End Function

    Public Function ListLines(runId As Integer) As IReadOnlyList(Of RunLineResult) Implements IPayrollRunRepository.ListLines
        Dim items As New List(Of RunLineResult)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_RunLine_List")
                cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New RunLineResult(reader.GetInt32(reader.GetOrdinal("EmployeeID")), reader.GetInt32(reader.GetOrdinal("FactoryID")),
                                                    Money(reader, "PaidDays"), Money(reader, "OvertimeHours"), Money(reader, "OvertimeAmount"),
                                                    Money(reader, "GrossEarnings"), Money(reader, "EpfWage"), Money(reader, "EsiWage"),
                                                    Money(reader, "EpfEmployee"), Money(reader, "EpfEmployerEpf"), Money(reader, "EpfEmployerEps"),
                                                    Money(reader, "Edli"), Money(reader, "EpfAdmin"), Money(reader, "EsiEmployee"), Money(reader, "EsiEmployer"),
                                                    Money(reader, "ProfessionalTax"), Money(reader, "LoanEmi"), Money(reader, "LoanEmiDeferred"),
                                                    Money(reader, "OtherDeductions"), Money(reader, "CarryForwardIn"), Money(reader, "CarryForwardOut"),
                                                    Money(reader, "TotalDeductions"), Money(reader, "NetPay"), Nothing,
                                                    Money(reader, "TdsAmount")))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Function ListEvents(runId As Integer) As IReadOnlyList(Of PayrollRunEventItem) Implements IPayrollRunRepository.ListEvents
        Dim items As New List(Of PayrollRunEventItem)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_RunEvent_List")
                cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim userOrdinal As Integer = reader.GetOrdinal("UserID")
                        Dim reasonOrdinal As Integer = reader.GetOrdinal("Reason")
                        items.Add(New PayrollRunEventItem(reader.GetInt64(reader.GetOrdinal("EventID")),
                                                          CInt(reader.GetByte(reader.GetOrdinal("FromStatusID"))),
                                                          CInt(reader.GetByte(reader.GetOrdinal("ToStatusID"))),
                                                          reader.GetString(reader.GetOrdinal("Action")),
                                                          If(reader.IsDBNull(userOrdinal), CType(Nothing, Integer?), reader.GetInt32(userOrdinal)),
                                                          If(reader.IsDBNull(reasonOrdinal), Nothing, reader.GetString(reasonOrdinal)),
                                                          reader.GetDateTime(reader.GetOrdinal("EventUtc"))))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Function LoadInputs(period As RunPeriod) As IReadOnlyList(Of RunEmployeeFacts) Implements IPayrollRunRepository.LoadInputs
        Dim installments As ILookup(Of Integer, OpenInstallment) = ListOpenInstallments(period.MonthKey).ToLookup(Function(i) i.EmployeeId, Function(i) i.Installment)
        Dim salaries As New Dictionary(Of Integer, List(Of SalaryLineView))()
        Dim facts As New List(Of RunEmployeeFacts)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_RunInput_List")
                cmd.Parameters.Add("@PeriodFrom", SqlDbType.Date).Value = period.PeriodFrom
                cmd.Parameters.Add("@PeriodTo", SqlDbType.Date).Value = period.PeriodTo
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    Dim rows As New List(Of EmployeeRow)()
                    While reader.Read()
                        rows.Add(ReadEmployeeRow(reader))
                    End While
                    If reader.NextResult() Then
                        While reader.Read()
                            Dim employeeId As Integer = reader.GetInt32(reader.GetOrdinal("EmployeeID"))
                            If Not salaries.ContainsKey(employeeId) Then salaries(employeeId) = New List(Of SalaryLineView)()
                            salaries(employeeId).Add(New SalaryLineView(reader.GetInt32(reader.GetOrdinal("ComponentID")),
                                                                        reader.GetString(reader.GetOrdinal("Code")),
                                                                        reader.GetString(reader.GetOrdinal("Name")),
                                                                        reader.GetBoolean(reader.GetOrdinal("IsBasic")),
                                                                        reader.GetBoolean(reader.GetOrdinal("IsDa")),
                                                                        reader.GetBoolean(reader.GetOrdinal("IsOtBase")),
                                                                        reader.GetDecimal(reader.GetOrdinal("Amount")),
                                                                        reader.GetBoolean(reader.GetOrdinal("IsHra"))))
                        End While
                    End If
                    For Each r As EmployeeRow In rows
                        Dim lines As List(Of SalaryLineView) = Nothing
                        salaries.TryGetValue(r.EmployeeId, lines)
                        facts.Add(New RunEmployeeFacts(r.EmployeeId, r.FactoryId, r.EmpCode, r.IsDailyWage, r.GenderId, r.PtStateId, r.IsEpfMember,
                                                       r.EpfOnHigherWage, r.EpsOnHigherWage, r.IsEsiMember, False, r.PaidDays, r.OvertimeHours,
                                                       r.HoursPerDay, r.UnmarkedDays, r.CarryForwardIn, 0D, lines, installments(r.EmployeeId).ToList()))
                    Next
                End Using
            End Using
        End Using
        Return facts
    End Function

    Public Sub ReplaceLines(runId As Integer, lines As IReadOnlyList(Of RunLineResult), userId As Integer?) Implements IPayrollRunRepository.ReplaceLines
        Dim linesJson As String = JsonSerializer.Serialize(lines.Select(Function(l) New With {
            .EmployeeID = l.EmployeeId, .FactoryID = l.FactoryId, .PaidDays = l.PaidDays, .OvertimeHours = l.OvertimeHours, .OvertimeAmount = l.OvertimeAmount,
            .GrossEarnings = l.GrossEarnings, .EpfWage = l.EpfWage, .EsiWage = l.EsiWage, .EpfEmployee = l.EpfEmployee, .EpfEmployerEpf = l.EpfEmployerEpf,
            .EpfEmployerEps = l.EpfEmployerEps, .Edli = l.Edli, .EpfAdmin = l.EpfAdmin, .EsiEmployee = l.EsiEmployee, .EsiEmployer = l.EsiEmployer,
            .ProfessionalTax = l.ProfessionalTax, .TdsAmount = l.TdsAmount, .LoanEmi = l.LoanEmi, .LoanEmiDeferred = l.LoanEmiDeferred, .OtherDeductions = l.OtherDeductions,
            .CarryForwardIn = l.CarryForwardIn, .CarryForwardOut = l.CarryForwardOut, .TotalDeductions = l.TotalDeductions, .NetPay = l.NetPay}).ToList())
        Dim earningsJson As String = JsonSerializer.Serialize(lines.SelectMany(Function(l) l.Earnings.Select(Function(e) New With {
            .EmployeeID = l.EmployeeId, .ComponentID = e.ComponentId, .Amount = e.Amount})).ToList())
        Execute("pay.usp_Run_ReplaceLines",
                Sub(cmd)
                    cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                    cmd.Parameters.Add("@LinesJson", SqlDbType.NVarChar, -1).Value = linesJson
                    cmd.Parameters.Add("@EarningsJson", SqlDbType.NVarChar, -1).Value = earningsJson
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Public Sub ChangeStatus(runId As Integer, toStatus As PayrollRunStatus, action As String, reason As String) Implements IPayrollRunRepository.ChangeStatus
        Execute("pay.usp_Run_ChangeStatus",
                Sub(cmd)
                    cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                    cmd.Parameters.Add("@ToStatusID", SqlDbType.TinyInt).Value = CByte(toStatus)
                    cmd.Parameters.Add("@Action", SqlDbType.NVarChar, MaxActionLength).Value = action
                    cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, MaxReasonLength).Value = StoredProcedure.DbValue(reason)
                End Sub)
    End Sub

    Public Function ListOpenInstallments(uptoMonthKey As Integer) As IReadOnlyList(Of EmployeeInstallment) Implements IPayrollRunRepository.ListOpenInstallments
        Dim items As New List(Of EmployeeInstallment)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_LoanInstallment_ListOpen")
                cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = DBNull.Value
                cmd.Parameters.Add("@UptoMonthKey", SqlDbType.Int).Value = uptoMonthKey
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New EmployeeInstallment(reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                                          New OpenInstallment(reader.GetInt32(reader.GetOrdinal("LoanID")),
                                                                              CInt(reader.GetInt16(reader.GetOrdinal("InstallmentNo"))),
                                                                              reader.GetInt32(reader.GetOrdinal("DueMonthKey")),
                                                                              reader.GetDecimal(reader.GetOrdinal("Outstanding")))))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Sub SaveRecoveries(runId As Integer, recoveries As IReadOnlyList(Of RecoveryAllocation)) Implements IPayrollRunRepository.SaveRecoveries
        Dim json As String = JsonSerializer.Serialize(recoveries.Select(Function(r) New With {.LoanID = r.LoanID, .InstallmentNo = r.InstallmentNo, .Amount = r.Amount}).ToList())
        Execute("pay.usp_Recovery_Save",
                Sub(cmd)
                    cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                    cmd.Parameters.Add("@RecoveriesJson", SqlDbType.NVarChar, -1).Value = json
                End Sub)
    End Sub

    Public Sub ReverseRecoveries(runId As Integer) Implements IPayrollRunRepository.ReverseRecoveries
        Execute("pay.usp_Recovery_ReverseRun",
                Sub(cmd)
                    cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                End Sub)
    End Sub

    Private NotInheritable Class EmployeeRow
        Public EmployeeId As Integer
        Public FactoryId As Integer
        Public EmpCode As String
        Public IsDailyWage As Boolean
        Public GenderId As Integer
        Public PtStateId As Integer?
        Public IsEpfMember As Boolean
        Public EpfOnHigherWage As Boolean
        Public EpsOnHigherWage As Boolean
        Public IsEsiMember As Boolean
        Public PaidDays As Decimal
        Public OvertimeHours As Decimal
        Public HoursPerDay As Decimal?
        Public UnmarkedDays As Integer
        Public CarryForwardIn As Decimal
    End Class

    Private Shared Function ReadEmployeeRow(reader As SqlDataReader) As EmployeeRow
        Dim ptOrdinal As Integer = reader.GetOrdinal("PtStateID")
        Dim hoursOrdinal As Integer = reader.GetOrdinal("HoursPerDay")
        Return New EmployeeRow With {
            .EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeID")),
            .FactoryId = reader.GetInt32(reader.GetOrdinal("FactoryID")),
            .EmpCode = reader.GetString(reader.GetOrdinal("EmpCode")),
            .IsDailyWage = reader.GetBoolean(reader.GetOrdinal("IsDailyWage")),
            .GenderId = reader.GetInt32(reader.GetOrdinal("GenderID")),
            .PtStateId = If(reader.IsDBNull(ptOrdinal), CType(Nothing, Integer?), reader.GetInt32(ptOrdinal)),
            .IsEpfMember = reader.GetBoolean(reader.GetOrdinal("IsEpfMember")),
            .EpfOnHigherWage = reader.GetBoolean(reader.GetOrdinal("EpfOnHigherWage")),
            .EpsOnHigherWage = reader.GetBoolean(reader.GetOrdinal("EpsOnHigherWage")),
            .IsEsiMember = reader.GetBoolean(reader.GetOrdinal("IsEsiMember")),
            .PaidDays = reader.GetDecimal(reader.GetOrdinal("PaidDays")),
            .OvertimeHours = reader.GetDecimal(reader.GetOrdinal("OvertimeHours")),
            .HoursPerDay = If(reader.IsDBNull(hoursOrdinal), CType(Nothing, Decimal?), reader.GetDecimal(hoursOrdinal)),
            .UnmarkedDays = reader.GetInt32(reader.GetOrdinal("UnmarkedDays")),
            .CarryForwardIn = reader.GetDecimal(reader.GetOrdinal("CarryForwardIn"))}
    End Function

    Private Shared Function Money(reader As SqlDataReader, column As String) As Decimal
        Return reader.GetDecimal(reader.GetOrdinal(column))
    End Function

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
