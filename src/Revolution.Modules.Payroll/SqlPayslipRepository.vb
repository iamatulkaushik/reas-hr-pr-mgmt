Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Payslip data through rpt.usp_Payslip_Get (ADR 0005, ADR 0008). Reads the Last4 columns and never the *Enc ciphertext columns. Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlPayslipRepository
    Implements IPayslipRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function GetPayslipRows(runId As Integer, employeeId As Integer?) As IReadOnlyList(Of PayslipRow) Implements IPayslipRepository.GetPayslipRows
        Dim rows As New List(Of PayslipRow)()
        Dim earnings As New Dictionary(Of Integer, List(Of PayslipEarning))()
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "rpt.usp_Payslip_Get")
                    cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = StoredProcedure.DbValue(employeeId)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            rows.Add(ReadRow(reader))
                        End While
                        If reader.NextResult() Then
                            While reader.Read()
                                Dim id As Integer = reader.GetInt32(reader.GetOrdinal("EmployeeID"))
                                If Not earnings.ContainsKey(id) Then earnings(id) = New List(Of PayslipEarning)()
                                earnings(id).Add(New PayslipEarning(reader.GetString(reader.GetOrdinal("ComponentCode")),
                                                                    reader.GetString(reader.GetOrdinal("ComponentName")),
                                                                    CInt(reader.GetInt16(reader.GetOrdinal("SortOrder"))),
                                                                    reader.GetDecimal(reader.GetOrdinal("Amount"))))
                            End While
                        End If
                    End Using
                End Using
            End Using
        Catch ex As SqlException When PayrollErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(PayrollErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
        For Each row As PayslipRow In rows
            Dim list As List(Of PayslipEarning) = Nothing
            If earnings.TryGetValue(row.EmployeeId, list) Then row.Earnings = list
        Next
        Return rows
    End Function

    ''' <summary>Reads a money column when the procedure returns it, otherwise 0. Lets the mapping ship before rpt.usp_Payslip_Get (migration 0701) returns TdsAmount.</summary>
    Private Shared Function OptionalDecimal(reader As SqlDataReader, column As String) As Decimal
        For index As Integer = 0 To reader.FieldCount - 1
            If String.Equals(reader.GetName(index), column, StringComparison.OrdinalIgnoreCase) Then Return If(reader.IsDBNull(index), 0D, reader.GetDecimal(index))
        Next
        Return 0D
    End Function

    Private Shared Function ReadRow(reader As SqlDataReader) As PayslipRow
        Return New PayslipRow With {
            .RunId = reader.GetInt32(reader.GetOrdinal("RunID")),
            .RunStatus = CType(CInt(reader.GetByte(reader.GetOrdinal("RunStatusID"))), PayrollRunStatus),
            .PeriodFrom = reader.GetDateTime(reader.GetOrdinal("PeriodFrom")),
            .PeriodTo = reader.GetDateTime(reader.GetOrdinal("PeriodTo")),
            .CompanyName = Text(reader, "CompanyName"),
            .CompanyAddress1 = Text(reader, "CompanyAddress1"),
            .CompanyAddress2 = Text(reader, "CompanyAddress2"),
            .FactoryName = Text(reader, "FactoryName"),
            .EpfSubCode = Text(reader, "EpfSubCode"),
            .EsiSubCode = Text(reader, "EsiSubCode"),
            .EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeID")),
            .EmpCode = Text(reader, "EmpCode"),
            .FullName = Text(reader, "FullName"),
            .FatherOrSpouseName = Text(reader, "FatherOrSpouseName"),
            .JoinDate = reader.GetDateTime(reader.GetOrdinal("JoinDate")),
            .PfNumber = Text(reader, "PfNumber"),
            .UanLast4 = Text(reader, "UanLast4"),
            .EsiIpLast4 = Text(reader, "EsiIpLast4"),
            .BankName = Text(reader, "BankName"),
            .Ifsc = Text(reader, "Ifsc"),
            .AccountLast4 = Text(reader, "AccountLast4"),
            .DesignationName = Text(reader, "DesignationName"),
            .DepartmentName = Text(reader, "DepartmentName"),
            .PaidDays = reader.GetDecimal(reader.GetOrdinal("PaidDays")),
            .OvertimeHours = reader.GetDecimal(reader.GetOrdinal("OvertimeHours")),
            .OvertimeAmount = reader.GetDecimal(reader.GetOrdinal("OvertimeAmount")),
            .GrossEarnings = reader.GetDecimal(reader.GetOrdinal("GrossEarnings")),
            .EpfEmployee = reader.GetDecimal(reader.GetOrdinal("EpfEmployee")),
            .EsiEmployee = reader.GetDecimal(reader.GetOrdinal("EsiEmployee")),
            .ProfessionalTax = reader.GetDecimal(reader.GetOrdinal("ProfessionalTax")),
            .TdsAmount = OptionalDecimal(reader, "TdsAmount"),
            .LoanEmi = reader.GetDecimal(reader.GetOrdinal("LoanEmi")),
            .OtherDeductions = reader.GetDecimal(reader.GetOrdinal("OtherDeductions")),
            .CarryForwardIn = reader.GetDecimal(reader.GetOrdinal("CarryForwardIn")),
            .TotalDeductions = reader.GetDecimal(reader.GetOrdinal("TotalDeductions")),
            .NetPay = reader.GetDecimal(reader.GetOrdinal("NetPay")),
            .LoanEmiDeferred = reader.GetDecimal(reader.GetOrdinal("LoanEmiDeferred")),
            .CarryForwardOut = reader.GetDecimal(reader.GetOrdinal("CarryForwardOut"))}
    End Function

    Private Shared Function Text(reader As SqlDataReader, column As String) As String
        Dim ordinal As Integer = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(ordinal), String.Empty, reader.GetString(ordinal))
    End Function
End Class
