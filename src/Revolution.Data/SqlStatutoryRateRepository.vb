Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Reads stat.* rows through stored procedures (ADR 0001, ADR 0010). Read only: rates arrive through migrations.</summary>
Public NotInheritable Class SqlStatutoryRateRepository
    Implements IStatutoryRateRepository

    Private Const VerificationSetting As String = "RateVerificationRequired"

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function GetEpfRate(asOf As Date) As EpfRate Implements IStatutoryRateRepository.GetEpfRate
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "stat.usp_EpfRate_Get")
                cmd.Parameters.Add("@AsOf", SqlDbType.Date).Value = asOf.Date
                Using r = cmd.ExecuteReader()
                    If Not r.Read() Then Return Nothing
                    Return New EpfRate(r.GetDateTime(r.GetOrdinal("EffectiveFrom")), r.GetDecimal(r.GetOrdinal("EmployeePct")),
                                       r.GetDecimal(r.GetOrdinal("EmployerEpfPct")), r.GetDecimal(r.GetOrdinal("EmployerEpsPct")),
                                       r.GetDecimal(r.GetOrdinal("EdliPct")), r.GetDecimal(r.GetOrdinal("AdminPct")),
                                       r.GetDecimal(r.GetOrdinal("AdminMinimum")), r.GetDecimal(r.GetOrdinal("WageCeiling")),
                                       r.GetDecimal(r.GetOrdinal("EdliWageCeiling")), r.GetBoolean(r.GetOrdinal("Verified")),
                                       Text(r, "SourceRef"))
                End Using
            End Using
        End Using
    End Function

    Public Function GetEsiRate(asOf As Date) As EsiRate Implements IStatutoryRateRepository.GetEsiRate
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "stat.usp_EsiRate_Get")
                cmd.Parameters.Add("@AsOf", SqlDbType.Date).Value = asOf.Date
                Using r = cmd.ExecuteReader()
                    If Not r.Read() Then Return Nothing
                    Return New EsiRate(r.GetDateTime(r.GetOrdinal("EffectiveFrom")), r.GetDecimal(r.GetOrdinal("EmployeePct")),
                                       r.GetDecimal(r.GetOrdinal("EmployerPct")), r.GetDecimal(r.GetOrdinal("WageCeiling")),
                                       r.GetDecimal(r.GetOrdinal("DailyWageExemption")),
                                       CType(CInt(r.GetByte(r.GetOrdinal("RoundingID"))), RateRounding),
                                       r.GetBoolean(r.GetOrdinal("Verified")), Text(r, "SourceRef"))
                End Using
            End Using
        End Using
    End Function

    Public Function GetPtRule(stateId As Integer, asOf As Date) As PtRule Implements IStatutoryRateRepository.GetPtRule
        Dim effectiveFrom As Date
        Dim frequency As PtFrequency
        Dim specialMonth As Integer?
        Dim verified As Boolean
        Dim sourceRef As String
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "stat.usp_PtRule_Get")
                cmd.Parameters.Add("@AsOf", SqlDbType.Date).Value = asOf.Date
                cmd.Parameters.Add("@StateID", SqlDbType.TinyInt).Value = CByte(stateId)
                Using r = cmd.ExecuteReader()
                    If Not r.Read() Then Return Nothing
                    effectiveFrom = r.GetDateTime(r.GetOrdinal("EffectiveFrom"))
                    frequency = CType(CInt(r.GetByte(r.GetOrdinal("FrequencyID"))), PtFrequency)
                    Dim monthOrdinal = r.GetOrdinal("SpecialMonth")
                    specialMonth = If(r.IsDBNull(monthOrdinal), CType(Nothing, Integer?), CInt(r.GetByte(monthOrdinal)))
                    verified = r.GetBoolean(r.GetOrdinal("Verified"))
                    sourceRef = Text(r, "SourceRef")
                End Using
            End Using
            Dim slabs As New List(Of PtSlab)()
            Using cmd = StoredProcedure.CreateCommand(conn, "stat.usp_PtSlab_List")
                cmd.Parameters.Add("@StateID", SqlDbType.TinyInt).Value = CByte(stateId)
                cmd.Parameters.Add("@AsOf", SqlDbType.Date).Value = asOf.Date
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim toOrdinal = r.GetOrdinal("ToAmount")
                        Dim specialOrdinal = r.GetOrdinal("SpecialMonthTax")
                        slabs.Add(New PtSlab(CInt(r.GetByte(r.GetOrdinal("GenderID"))), CInt(r.GetByte(r.GetOrdinal("SlabNo"))),
                                             r.GetDecimal(r.GetOrdinal("FromAmount")),
                                             If(r.IsDBNull(toOrdinal), CType(Nothing, Decimal?), r.GetDecimal(toOrdinal)),
                                             r.GetDecimal(r.GetOrdinal("TaxAmount")),
                                             If(r.IsDBNull(specialOrdinal), CType(Nothing, Decimal?), r.GetDecimal(specialOrdinal))))
                    End While
                End Using
            End Using
            Return New PtRule(stateId, effectiveFrom, frequency, specialMonth, verified, sourceRef, slabs)
        End Using
    End Function

    Public Function GetWageRule(asOf As Date) As WageRule Implements IStatutoryRateRepository.GetWageRule
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "stat.usp_WageRule_Get")
                cmd.Parameters.Add("@AsOf", SqlDbType.Date).Value = asOf.Date
                Using r = cmd.ExecuteReader()
                    If Not r.Read() Then Return Nothing
                    Return New WageRule(r.GetDateTime(r.GetOrdinal("EffectiveFrom")), r.GetBoolean(r.GetOrdinal("UseCodeWage")),
                                        r.GetDecimal(r.GetOrdinal("WageFloorPct")), Text(r, "Notes"), r.GetBoolean(r.GetOrdinal("Verified")),
                                        Text(r, "SourceRef"))
                End Using
            End Using
        End Using
    End Function

    Public Function IsVerificationRequired() As Boolean Implements IStatutoryRateRepository.IsVerificationRequired
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "cfg.usp_Setting_Get")
                cmd.Parameters.Add("@SettingKey", SqlDbType.NVarChar, 100).Value = VerificationSetting
                Dim value = cmd.ExecuteScalar()
                Return value Is Nothing OrElse value Is DBNull.Value OrElse Convert.ToString(value) <> "0"
            End Using
        End Using
    End Function

    Private Shared Function Text(r As SqlDataReader, column As String) As String
        Dim i = r.GetOrdinal(column)
        Return If(r.IsDBNull(i), String.Empty, r.GetString(i))
    End Function
End Class
