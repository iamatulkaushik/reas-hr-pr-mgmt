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
            Dim dueMonths As New List(Of Integer)()
            Using cmd = StoredProcedure.CreateCommand(conn, "stat.usp_PtDueMonth_List")
                cmd.Parameters.Add("@StateID", SqlDbType.TinyInt).Value = CByte(stateId)
                cmd.Parameters.Add("@AsOf", SqlDbType.Date).Value = asOf.Date
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        dueMonths.Add(CInt(r.GetByte(r.GetOrdinal("MonthNo"))))
                    End While
                End Using
            End Using
            Return New PtRule(stateId, effectiveFrom, frequency, specialMonth, verified, sourceRef, slabs, dueMonths)
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

    Public Function GetGratuityRule(asOf As Date) As GratuityRule Implements IStatutoryRateRepository.GetGratuityRule
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "stat.usp_GratuityRule_Get")
                cmd.Parameters.Add("@AsOf", SqlDbType.Date).Value = asOf.Date
                Using r = cmd.ExecuteReader()
                    If Not r.Read() Then Return Nothing
                    Dim verifiedOn = r.GetOrdinal("VerifiedOn")
                    Return New GratuityRule(r.GetDateTime(r.GetOrdinal("EffectiveFrom")), CInt(r.GetByte(r.GetOrdinal("MinYearsRegular"))),
                                            CInt(r.GetByte(r.GetOrdinal("MinYearsFixedTerm"))), r.GetDecimal(r.GetOrdinal("DaysPerYear")),
                                            r.GetDecimal(r.GetOrdinal("MonthDivisor")), CInt(r.GetByte(r.GetOrdinal("RoundUpAfterMonths"))),
                                            r.GetDecimal(r.GetOrdinal("CapAmount")), r.GetBoolean(r.GetOrdinal("Verified")),
                                            If(r.IsDBNull(verifiedOn), CType(Nothing, Date?), r.GetDateTime(verifiedOn)), Text(r, "SourceRef"))
                End Using
            End Using
        End Using
    End Function

    Public Function GetBonusRule(asOf As Date) As BonusRule Implements IStatutoryRateRepository.GetBonusRule
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "stat.usp_BonusRule_Get")
                cmd.Parameters.Add("@AsOf", SqlDbType.Date).Value = asOf.Date
                Using r = cmd.ExecuteReader()
                    If Not r.Read() Then Return Nothing
                    Dim verifiedOn = r.GetOrdinal("VerifiedOn")
                    Return New BonusRule(r.GetDateTime(r.GetOrdinal("EffectiveFrom")), r.GetDecimal(r.GetOrdinal("EligibilityWageCeiling")),
                                         r.GetDecimal(r.GetOrdinal("CalcWageCeiling")), r.GetDecimal(r.GetOrdinal("MinPct")),
                                         r.GetDecimal(r.GetOrdinal("MaxPct")), CInt(r.GetInt16(r.GetOrdinal("MinDaysWorked"))),
                                         r.GetBoolean(r.GetOrdinal("Verified")),
                                         If(r.IsDBNull(verifiedOn), CType(Nothing, Date?), r.GetDateTime(verifiedOn)), Text(r, "SourceRef"))
                End Using
            End Using
        End Using
    End Function

    Public Function GetTdsDeductionLimits(financialYear As Integer, regime As TdsRegime) As IReadOnlyList(Of TdsDeductionLimit) Implements IStatutoryRateRepository.GetTdsDeductionLimits
        Dim limits As New List(Of TdsDeductionLimit)()
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "stat.usp_TdsDeductionLimit_List")
                cmd.Parameters.Add("@FinancialYear", SqlDbType.SmallInt).Value = CShort(financialYear)
                cmd.Parameters.Add("@RegimeID", SqlDbType.TinyInt).Value = CByte(regime)
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim verifiedOn = r.GetOrdinal("VerifiedOn")
                        limits.Add(New TdsDeductionLimit(financialYear, regime, Text(r, "SectionCode"), Text(r, "SubLimitKey"),
                                                         r.GetDecimal(r.GetOrdinal("MaxAmount")), Text(r, "Note"), r.GetBoolean(r.GetOrdinal("Verified")),
                                                         If(r.IsDBNull(verifiedOn), CType(Nothing, Date?), r.GetDateTime(verifiedOn)), Text(r, "SourceRef")))
                    End While
                End Using
            End Using
        End Using
        Return limits
    End Function

    Public Function GetHraCities(asOf As Date) As IReadOnlyList(Of HraCity) Implements IStatutoryRateRepository.GetHraCities
        Dim cities As New List(Of HraCity)()
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "stat.usp_HraMetroCity_List")
                cmd.Parameters.Add("@AsOf", SqlDbType.Date).Value = asOf.Date
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim verifiedOn = r.GetOrdinal("VerifiedOn")
                        cities.Add(New HraCity(r.GetDateTime(r.GetOrdinal("EffectiveFrom")), Text(r, "CityName"), r.GetBoolean(r.GetOrdinal("IsMetro")),
                                               r.GetDecimal(r.GetOrdinal("ExemptPct")), r.GetBoolean(r.GetOrdinal("Verified")),
                                               If(r.IsDBNull(verifiedOn), CType(Nothing, Date?), r.GetDateTime(verifiedOn)), Text(r, "SourceRef")))
                    End While
                End Using
            End Using
        End Using
        Return cities
    End Function

    Public Function GetTdsRule(financialYear As Integer, regime As TdsRegime, ageBand As TdsAgeBand) As TdsRule Implements IStatutoryRateRepository.GetTdsRule
        Dim standardDeduction As Decimal
        Dim rebateLimit As Decimal
        Dim rebateMaximum As Decimal
        Dim cessPct As Decimal
        Dim verified As Boolean
        Dim sourceRef As String
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "stat.usp_TdsParam_Get")
                cmd.Parameters.Add("@FinancialYear", SqlDbType.SmallInt).Value = CShort(financialYear)
                cmd.Parameters.Add("@RegimeID", SqlDbType.TinyInt).Value = CByte(regime)
                Using r = cmd.ExecuteReader()
                    If Not r.Read() Then Return Nothing
                    standardDeduction = r.GetDecimal(r.GetOrdinal("StandardDeduction"))
                    rebateLimit = r.GetDecimal(r.GetOrdinal("RebateIncomeLimit"))
                    rebateMaximum = r.GetDecimal(r.GetOrdinal("RebateMaximum"))
                    cessPct = r.GetDecimal(r.GetOrdinal("CessPct"))
                    verified = r.GetBoolean(r.GetOrdinal("Verified"))
                    sourceRef = Text(r, "SourceRef")
                End Using
            End Using
            Dim slabs As New List(Of TdsSlab)()
            Using cmd = StoredProcedure.CreateCommand(conn, "stat.usp_TdsSlab_List")
                cmd.Parameters.Add("@FinancialYear", SqlDbType.SmallInt).Value = CShort(financialYear)
                cmd.Parameters.Add("@RegimeID", SqlDbType.TinyInt).Value = CByte(regime)
                cmd.Parameters.Add("@AgeBandID", SqlDbType.TinyInt).Value = CByte(ageBand)
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim toOrdinal = r.GetOrdinal("ToAmount")
                        slabs.Add(New TdsSlab(CInt(r.GetByte(r.GetOrdinal("SlabNo"))), r.GetDecimal(r.GetOrdinal("FromAmount")),
                                              If(r.IsDBNull(toOrdinal), CType(Nothing, Decimal?), r.GetDecimal(toOrdinal)),
                                              r.GetDecimal(r.GetOrdinal("RatePct"))))
                    End While
                End Using
            End Using
            Dim surcharges As New List(Of TdsSurchargeBand)()
            Using cmd = StoredProcedure.CreateCommand(conn, "stat.usp_TdsSurcharge_List")
                cmd.Parameters.Add("@FinancialYear", SqlDbType.SmallInt).Value = CShort(financialYear)
                cmd.Parameters.Add("@RegimeID", SqlDbType.TinyInt).Value = CByte(regime)
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        Dim toOrdinal = r.GetOrdinal("ToIncome")
                        surcharges.Add(New TdsSurchargeBand(CInt(r.GetByte(r.GetOrdinal("SlabNo"))), r.GetDecimal(r.GetOrdinal("FromIncome")),
                                                            If(r.IsDBNull(toOrdinal), CType(Nothing, Decimal?), r.GetDecimal(toOrdinal)),
                                                            r.GetDecimal(r.GetOrdinal("RatePct"))))
                    End While
                End Using
            End Using
            Return New TdsRule(financialYear, regime, ageBand, standardDeduction, rebateLimit, rebateMaximum, cessPct, verified,
                               sourceRef, slabs, surcharges)
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
