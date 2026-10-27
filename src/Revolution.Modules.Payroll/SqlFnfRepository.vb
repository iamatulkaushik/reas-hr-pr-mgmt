Imports System.Data
Imports System.Globalization
Imports System.Text.Json
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>F&amp;F storage through pay.usp_Fnf_Save, usp_Fnf_ChangeStatus and usp_Fnf_Get (ADR 0001, ADR 0008). Tenant and user context come from IConnectionFactory.</summary>
Public NotInheritable Class SqlFnfRepository
    Implements IFnfRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function Save(separationId As Integer, lines As IReadOnlyList(Of FnfLine)) As Integer Implements IFnfRepository.Save
        Dim json As String = JsonSerializer.Serialize(lines.Select(Function(l) New With {.Type = CInt(l.Kind), .Description = l.Description, .Amount = l.Amount}))
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Fnf_Save")
                    cmd.Parameters.Add("@SeparationID", SqlDbType.Int).Value = separationId
                    cmd.Parameters.Add("@ComponentsJson", SqlDbType.NVarChar, -1).Value = json
                    Return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture)
                End Using
            End Using
        Catch ex As SqlException When PayrollErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(PayrollErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
    End Function

    Public Sub ChangeStatus(fnfId As Integer, newStatus As FnfStatus) Implements IFnfRepository.ChangeStatus
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Fnf_ChangeStatus")
                    cmd.Parameters.Add("@FnfID", SqlDbType.Int).Value = fnfId
                    cmd.Parameters.Add("@NewStatusID", SqlDbType.TinyInt).Value = CByte(newStatus)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When PayrollErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(PayrollErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
    End Sub

    Public Function [Get](separationId As Integer) As FnfSettlementView Implements IFnfRepository.Get
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Fnf_Get")
                cmd.Parameters.Add("@SeparationID", SqlDbType.Int).Value = separationId
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    If Not reader.Read() Then Return Nothing
                    Dim header As FnfSettlementView = ReadHeader(reader)
                    Dim lines As New List(Of FnfLine)()
                    If reader.NextResult() Then
                        While reader.Read()
                            lines.Add(New FnfLine(CType(CInt(reader.GetByte(reader.GetOrdinal("ComponentTypeID"))), FnfComponentKind),
                                                  reader.GetString(reader.GetOrdinal("Description")), reader.GetDecimal(reader.GetOrdinal("Amount"))))
                        End While
                    End If
                    Return New FnfSettlementView(header.FnfId, header.EmployeeId, header.SeparationId, header.ExitDate, header.Status, header.Totals,
                                                 header.MakerUserId, header.CheckerUserId, lines)
                End Using
            End Using
        End Using
    End Function

    Private Shared Function ReadHeader(reader As SqlDataReader) As FnfSettlementView
        Return New FnfSettlementView(reader.GetInt32(reader.GetOrdinal("FnfID")), reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                     reader.GetInt32(reader.GetOrdinal("SeparationID")), reader.GetDateTime(reader.GetOrdinal("ExitDate")),
                                     CType(CInt(reader.GetByte(reader.GetOrdinal("StatusID"))), FnfStatus),
                                     New FnfTotals(reader.GetDecimal(reader.GetOrdinal("TotalEarnings")), reader.GetDecimal(reader.GetOrdinal("TotalDeductions"))),
                                     ReadId(reader, "MakerUserID"), ReadId(reader, "CheckerUserID"), New List(Of FnfLine)())
    End Function

    Private Shared Function ReadId(reader As SqlDataReader, column As String) As Integer?
        Dim ordinal As Integer = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(ordinal), CType(Nothing, Integer?), reader.GetInt32(ordinal))
    End Function
End Class
