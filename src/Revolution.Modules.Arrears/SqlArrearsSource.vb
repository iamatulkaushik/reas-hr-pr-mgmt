Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data
Imports Revolution.Modules.Payroll

''' <summary>Finds the regular run for a pay period through pay.usp_ArrearsRun_Find (ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlArrearsSource
    Implements IArrearsSource

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function NextRunNo(period As RunPeriod) As Integer Implements IArrearsSource.NextRunNo
        If period Is Nothing Then Throw New ArgumentNullException(NameOf(period))
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_ArrearsRun_NextNo")
                cmd.Parameters.Add("@PeriodFrom", SqlDbType.Date).Value = period.PeriodFrom.Date
                Return Convert.ToInt32(cmd.ExecuteScalar(), Globalization.CultureInfo.InvariantCulture)
            End Using
        End Using
    End Function

    Public Function FindRegularRun(period As RunPeriod) As PayrollRunHeader Implements IArrearsSource.FindRegularRun
        If period Is Nothing Then Throw New ArgumentNullException(NameOf(period))
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_ArrearsRun_Find")
                cmd.Parameters.Add("@PeriodFrom", SqlDbType.Date).Value = period.PeriodFrom.Date
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    If Not reader.Read() Then Return Nothing
                    Return New PayrollRunHeader(reader.GetInt32(reader.GetOrdinal("RunID")), reader.GetDateTime(reader.GetOrdinal("PeriodFrom")),
                                                reader.GetDateTime(reader.GetOrdinal("PeriodTo")), CInt(reader.GetByte(reader.GetOrdinal("RunNo"))),
                                                CType(CInt(reader.GetByte(reader.GetOrdinal("StatusID"))), PayrollRunStatus), reader.GetInt32(reader.GetOrdinal("EmployeeCount")),
                                                reader.GetDecimal(reader.GetOrdinal("TotalGross")), reader.GetDecimal(reader.GetOrdinal("TotalDeductions")),
                                                reader.GetDecimal(reader.GetOrdinal("TotalNet")))
                End Using
            End Using
        End Using
    End Function
End Class
