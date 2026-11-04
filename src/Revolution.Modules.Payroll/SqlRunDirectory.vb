Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Run list through pay.usp_Run_ListForPeriod (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlRunDirectory
    Implements IRunDirectory

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function ListForPeriod(period As RunPeriod) As IReadOnlyList(Of PayrollRunHeader) Implements IRunDirectory.ListForPeriod
        If period Is Nothing Then Throw New ArgumentNullException(NameOf(period))
        Dim items As New List(Of PayrollRunHeader)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Run_ListForPeriod")
                cmd.Parameters.Add("@PeriodFrom", SqlDbType.Date).Value = period.PeriodFrom.Date
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New PayrollRunHeader(reader.GetInt32(reader.GetOrdinal("RunID")), reader.GetDateTime(reader.GetOrdinal("PeriodFrom")),
                                                       reader.GetDateTime(reader.GetOrdinal("PeriodTo")), CInt(reader.GetByte(reader.GetOrdinal("RunNo"))),
                                                       CType(CInt(reader.GetByte(reader.GetOrdinal("StatusID"))), PayrollRunStatus), reader.GetInt32(reader.GetOrdinal("EmployeeCount")),
                                                       reader.GetDecimal(reader.GetOrdinal("TotalGross")), reader.GetDecimal(reader.GetOrdinal("TotalDeductions")),
                                                       reader.GetDecimal(reader.GetOrdinal("TotalNet"))))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function
End Class
