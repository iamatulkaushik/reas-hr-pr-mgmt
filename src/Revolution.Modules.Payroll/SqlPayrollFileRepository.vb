Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Run header and lines for file export through pay.usp_Run_Get, pay.usp_RunFile_Employees and pay.usp_Run_List (ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlPayrollFileRepository
    Implements IPayrollFileRepository
    Implements IPayrollRunLister

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function GetRun(runId As Integer) As PayrollFileRun Implements IPayrollFileRepository.GetRun
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Run_Get")
                cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    If Not reader.Read() Then Return Nothing
                    Return New PayrollFileRun(reader.GetInt32(reader.GetOrdinal("RunID")),
                                              reader.GetDateTime(reader.GetOrdinal("PeriodFrom")),
                                              reader.GetDateTime(reader.GetOrdinal("PeriodTo")),
                                              CType(CInt(reader.GetByte(reader.GetOrdinal("StatusID"))), PayrollRunStatus))
                End Using
            End Using
        End Using
    End Function

    Public Function GetRows(runId As Integer) As IReadOnlyList(Of PayrollFileRow) Implements IPayrollFileRepository.GetRows
        Dim rows As New List(Of PayrollFileRow)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_RunFile_Employees")
                cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        rows.Add(ReadRow(reader))
                    End While
                End Using
            End Using
        End Using
        Return rows
    End Function

    Public Function ListRuns() As IReadOnlyList(Of PayrollRunItem) Implements IPayrollRunLister.ListRuns
        Dim runs As New List(Of PayrollRunItem)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Run_List")
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        runs.Add(New PayrollRunItem(reader.GetInt32(reader.GetOrdinal("RunID")),
                                                    reader.GetDateTime(reader.GetOrdinal("PeriodFrom")),
                                                    reader.GetDateTime(reader.GetOrdinal("PeriodTo")),
                                                    CInt(reader.GetByte(reader.GetOrdinal("RunNo"))),
                                                    CType(CInt(reader.GetByte(reader.GetOrdinal("StatusID"))), PayrollRunStatus),
                                                    reader.GetInt32(reader.GetOrdinal("EmployeeCount"))))
                    End While
                End Using
            End Using
        End Using
        Return runs
    End Function

    Private Shared Function ReadRow(reader As SqlDataReader) As PayrollFileRow
        Return New PayrollFileRow With {
            .EmployeeID = reader.GetInt32(reader.GetOrdinal("EmployeeID")),
            .EmployeeCode = reader.GetString(reader.GetOrdinal("EmpCode")),
            .Name = reader.GetString(reader.GetOrdinal("FullName")),
            .PaidDays = reader.GetDecimal(reader.GetOrdinal("PaidDays")),
            .GrossEarnings = reader.GetDecimal(reader.GetOrdinal("GrossEarnings")),
            .EpfWage = reader.GetDecimal(reader.GetOrdinal("EpfWage")),
            .EsiWage = reader.GetDecimal(reader.GetOrdinal("EsiWage")),
            .EpfEmployee = reader.GetDecimal(reader.GetOrdinal("EpfEmployee")),
            .EpfEmployerEpf = reader.GetDecimal(reader.GetOrdinal("EpfEmployerEpf")),
            .EpfEmployerEps = reader.GetDecimal(reader.GetOrdinal("EpfEmployerEps")),
            .Edli = reader.GetDecimal(reader.GetOrdinal("Edli")),
            .EsiEmployee = reader.GetDecimal(reader.GetOrdinal("EsiEmployee")),
            .EsiEmployer = reader.GetDecimal(reader.GetOrdinal("EsiEmployer")),
            .NetPay = reader.GetDecimal(reader.GetOrdinal("NetPay")),
            .IsEpfMember = reader.GetBoolean(reader.GetOrdinal("IsEpfMember")),
            .EpsOnHigherWage = reader.GetBoolean(reader.GetOrdinal("EpsOnHigherWage")),
            .IsEsiMember = reader.GetBoolean(reader.GetOrdinal("IsEsiMember")),
            .PaysByBank = reader.GetBoolean(reader.GetOrdinal("PaysByBank")),
            .Ifsc = OptionalText(reader, "Ifsc"),
            .HasAccount = reader.GetBoolean(reader.GetOrdinal("HasAccount")),
            .ExitDate = OptionalDate(reader, "ExitDate")}
    End Function

    Private Shared Function OptionalText(reader As SqlDataReader, column As String) As String
        Dim i As Integer = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(i), String.Empty, reader.GetString(i).Trim())
    End Function

    Private Shared Function OptionalDate(reader As SqlDataReader, column As String) As Date?
        Dim i As Integer = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(i), CType(Nothing, Date?), reader.GetDateTime(i))
    End Function
End Class
