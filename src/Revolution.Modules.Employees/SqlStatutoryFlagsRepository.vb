Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Statutory flag storage through mst.usp_EmployeeStatutory_* (ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlStatutoryFlagsRepository
    Implements IStatutoryFlagsRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function GetFlags(employeeId As Integer) As StatutoryFlags Implements IStatutoryFlagsRepository.GetFlags
        Dim found As StatutoryFlags = Nothing
        EmployeeSql.Run(Sub()
                            Using conn = _factory.Open()
                                Using cmd = StoredProcedure.CreateCommand(conn, "mst.usp_EmployeeStatutory_Get")
                                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                                    Using reader = cmd.ExecuteReader()
                                        If reader.Read() Then
                                            found = ReadFlags(reader)
                                        End If
                                    End Using
                                End Using
                            End Using
                        End Sub)
        Return found
    End Function

    Public Sub SaveFlags(employeeId As Integer, flags As StatutoryFlags, userId As Integer?) Implements IStatutoryFlagsRepository.SaveFlags
        If flags Is Nothing Then Throw New ArgumentNullException(NameOf(flags))
        EmployeeSql.Execute(_factory, "mst.usp_EmployeeStatutory_SaveFlags",
                            Sub(cmd)
                                cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                                cmd.Parameters.Add("@IsEpfMember", SqlDbType.Bit).Value = flags.IsEpfMember
                                cmd.Parameters.Add("@PfNumber", SqlDbType.NVarChar, 30).Value = OptionalText(flags.PfNumber)
                                cmd.Parameters.Add("@EpfJoinDate", SqlDbType.Date).Value = If(flags.EpfJoinDate.HasValue, CObj(flags.EpfJoinDate.Value.Date), DBNull.Value)
                                cmd.Parameters.Add("@IsPensionMember", SqlDbType.Bit).Value = flags.IsPensionMember
                                cmd.Parameters.Add("@HigherWageOption", SqlDbType.Bit).Value = flags.HigherWageOption
                                cmd.Parameters.Add("@IsEsiMember", SqlDbType.Bit).Value = flags.IsEsiMember
                                cmd.Parameters.Add("@EsiDispensary", SqlDbType.NVarChar, 100).Value = OptionalText(flags.EsiDispensary)
                                cmd.Parameters.Add("@PtApplicable", SqlDbType.Bit).Value = flags.PtApplicable
                                cmd.Parameters.Add("@LwfApplicable", SqlDbType.Bit).Value = flags.LwfApplicable
                                cmd.Parameters.Add("@TaxRegimeID", SqlDbType.TinyInt).Value = CByte(flags.TaxRegimeId)
                                cmd.Parameters.Add("@IsInternationalWorker", SqlDbType.Bit).Value = flags.IsInternationalWorker
                                cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                            End Sub)
    End Sub

    Private Shared Function OptionalText(text As String) As Object
        Return If(String.IsNullOrWhiteSpace(text), CObj(DBNull.Value), text.Trim())
    End Function

    Private Shared Function ReadFlags(reader As SqlDataReader) As StatutoryFlags
        Return New StatutoryFlags With {
            .IsEpfMember = reader.GetBoolean(reader.GetOrdinal("IsEpfMember")),
            .PfNumber = EmployeeSql.OptText(reader, "PfNumber"),
            .EpfJoinDate = EmployeeSql.OptDate(reader, "EpfJoinDate"),
            .IsPensionMember = reader.GetBoolean(reader.GetOrdinal("IsPensionMember")),
            .HigherWageOption = reader.GetBoolean(reader.GetOrdinal("HigherWageOption")),
            .IsEsiMember = reader.GetBoolean(reader.GetOrdinal("IsEsiMember")),
            .EsiDispensary = EmployeeSql.OptText(reader, "EsiDispensary"),
            .PtApplicable = reader.GetBoolean(reader.GetOrdinal("PtApplicable")),
            .LwfApplicable = reader.GetBoolean(reader.GetOrdinal("LwfApplicable")),
            .TaxRegimeId = reader.GetByte(reader.GetOrdinal("TaxRegimeID")),
            .IsInternationalWorker = reader.GetBoolean(reader.GetOrdinal("IsInternationalWorker"))}
    End Function
End Class
