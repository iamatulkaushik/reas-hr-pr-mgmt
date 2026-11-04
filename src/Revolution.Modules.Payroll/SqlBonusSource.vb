Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Bonus run storage through pay.usp_Bonus_Facts, usp_BonusRun_Register and usp_ArrearsRun_NextNo (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlBonusSource
    Implements IBonusSource

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function Load(yearFrom As Date) As BonusFacts Implements IBonusSource.Load
        Dim months As New List(Of BonusMonthFact)()
        Dim paid As New Dictionary(Of Integer, Decimal)()
        Dim flags As New Dictionary(Of Integer, BonusEmployeeFlags)()
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Bonus_Facts")
                    cmd.Parameters.Add("@YearFrom", SqlDbType.Date).Value = yearFrom.Date
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            months.Add(New BonusMonthFact(reader.GetInt32(reader.GetOrdinal("EmployeeID")), reader.GetString(reader.GetOrdinal("EmpCode")),
                                                          reader.GetInt32(reader.GetOrdinal("FactoryID")), reader.GetInt32(reader.GetOrdinal("MonthKey")),
                                                          reader.GetDecimal(reader.GetOrdinal("BasicPlusDa")), reader.GetDecimal(reader.GetOrdinal("PaidDays"))))
                        End While
                        If reader.NextResult() Then
                            While reader.Read()
                                paid(reader.GetInt32(reader.GetOrdinal("EmployeeID"))) = reader.GetDecimal(reader.GetOrdinal("Amount"))
                            End While
                        End If
                        If reader.NextResult() Then
                            While reader.Read()
                                Dim id As Integer = reader.GetInt32(reader.GetOrdinal("EmployeeID"))
                                flags(id) = New BonusEmployeeFlags(id, reader.GetBoolean(reader.GetOrdinal("IsEpfMember")), reader.GetBoolean(reader.GetOrdinal("EpfOnHigherWage")),
                                                                   reader.GetBoolean(reader.GetOrdinal("EpsOnHigherWage")))
                            End While
                        End If
                    End Using
                End Using
            End Using
        Catch ex As SqlException When PayrollErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(PayrollErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
        Return New BonusFacts(months, paid, flags)
    End Function

    Public Function NextRunNo(period As RunPeriod) As Integer Implements IBonusSource.NextRunNo
        If period Is Nothing Then Throw New ArgumentNullException(NameOf(period))
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_ArrearsRun_NextNo")
                cmd.Parameters.Add("@PeriodFrom", SqlDbType.Date).Value = period.PeriodFrom.Date
                Return Convert.ToInt32(cmd.ExecuteScalar(), Globalization.CultureInfo.InvariantCulture)
            End Using
        End Using
    End Function

    Public Sub Register(runId As Integer, yearFrom As Date, pct As Decimal, userId As Integer?) Implements IBonusSource.Register
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_BonusRun_Register")
                    cmd.Parameters.Add("@RunID", SqlDbType.Int).Value = runId
                    cmd.Parameters.Add("@YearFrom", SqlDbType.Date).Value = yearFrom.Date
                    cmd.Parameters.Add("@Pct", SqlDbType.Decimal).Value = pct
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When PayrollErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(PayrollErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
    End Sub
End Class
