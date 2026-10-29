Imports System.Data
Imports System.Globalization
Imports System.Text.Json
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Salary structure storage through pay.usp_Policy_*, usp_Component_* and usp_Salary_* (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlSalaryRepository
    Implements ISalaryRepository

    Private Const MaxCodeLength As Integer = 20
    Private Const MaxNameLength As Integer = 100
    Private Const MaxReasonLength As Integer = 200

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function GetPolicy() As PolicySettings Implements ISalaryRepository.GetPolicy
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Policy_Get")
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    If Not reader.Read() Then Throw New InvalidOperationException("pay.usp_Policy_Get returned no row.")
                    Return New PolicySettings(CInt(reader.GetByte(reader.GetOrdinal("PayCycleStartDay"))),
                                              reader.GetDecimal(reader.GetOrdinal("OtMultiplier")),
                                              reader.GetBoolean(reader.GetOrdinal("OtInEpfWage")),
                                              reader.GetBoolean(reader.GetOrdinal("OtInEsiWage")),
                                              reader.GetDecimal(reader.GetOrdinal("DailyWageDivisor")),
                                              reader.GetDecimal(reader.GetOrdinal("EmiCapPct")))
                End Using
            End Using
        End Using
    End Function

    Public Sub SavePolicy(policy As PolicySettings, userId As Integer?) Implements ISalaryRepository.SavePolicy
        Execute("pay.usp_Policy_Save",
                Sub(cmd)
                    cmd.Parameters.Add("@PayCycleStartDay", SqlDbType.TinyInt).Value = CByte(policy.PayCycleStartDay)
                    AddDecimal(cmd, "@OtMultiplier", 7, 4, policy.OtMultiplier)
                    cmd.Parameters.Add("@OtInEpfWage", SqlDbType.Bit).Value = policy.OtInEpfWage
                    cmd.Parameters.Add("@OtInEsiWage", SqlDbType.Bit).Value = policy.OtInEsiWage
                    AddDecimal(cmd, "@DailyWageDivisor", 7, 2, policy.DailyWageDivisor)
                    AddDecimal(cmd, "@EmiCapPct", 7, 4, policy.EmiCapPct)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Public Function ListComponents() As IReadOnlyList(Of SalaryComponentItem) Implements ISalaryRepository.ListComponents
        Dim items As New List(Of SalaryComponentItem)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Component_List")
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New SalaryComponentItem(reader.GetInt32(reader.GetOrdinal("ComponentID")),
                                                          reader.GetString(reader.GetOrdinal("Code")),
                                                          reader.GetString(reader.GetOrdinal("Name")),
                                                          reader.GetBoolean(reader.GetOrdinal("IsBasic")),
                                                          reader.GetBoolean(reader.GetOrdinal("IsDa")),
                                                          reader.GetBoolean(reader.GetOrdinal("IsOtBase")),
                                                          CInt(reader.GetInt16(reader.GetOrdinal("SortOrder"))),
                                                          reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                                          reader.GetBoolean(reader.GetOrdinal("IsHra"))))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Function SaveComponent(component As SalaryComponentItem) As Integer Implements ISalaryRepository.SaveComponent
        Return ExecuteScalarInt("pay.usp_Component_Save",
                                Sub(cmd)
                                    cmd.Parameters.Add("@ComponentID", SqlDbType.Int).Value = StoredProcedure.DbValue(component.ComponentId)
                                    cmd.Parameters.Add("@Code", SqlDbType.NVarChar, MaxCodeLength).Value = component.Code
                                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, MaxNameLength).Value = component.Name
                                    cmd.Parameters.Add("@IsBasic", SqlDbType.Bit).Value = component.IsBasic
                                    cmd.Parameters.Add("@IsDa", SqlDbType.Bit).Value = component.IsDa
                                    cmd.Parameters.Add("@IsOtBase", SqlDbType.Bit).Value = component.IsOtBase
                                    cmd.Parameters.Add("@SortOrder", SqlDbType.SmallInt).Value = CShort(component.SortOrder)
                                    cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = component.IsActive
                                    cmd.Parameters.Add("@IsHra", SqlDbType.Bit).Value = component.IsHra
                                End Sub)
    End Function

    Public Function GetSalaryAt(employeeId As Integer, asOf As Date) As EmployeeSalaryView Implements ISalaryRepository.GetSalaryAt
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_Salary_GetAt")
                cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                cmd.Parameters.Add("@AsOf", SqlDbType.Date).Value = asOf
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    If Not reader.Read() Then Return Nothing
                    Dim salaryId As Integer = reader.GetInt32(reader.GetOrdinal("SalaryID"))
                    Dim effectiveFrom As Date = reader.GetDateTime(reader.GetOrdinal("EffectiveFrom"))
                    Dim lines As New List(Of SalaryLineView)()
                    If reader.NextResult() Then
                        While reader.Read()
                            lines.Add(New SalaryLineView(reader.GetInt32(reader.GetOrdinal("ComponentID")),
                                                         reader.GetString(reader.GetOrdinal("Code")),
                                                         reader.GetString(reader.GetOrdinal("Name")),
                                                         reader.GetBoolean(reader.GetOrdinal("IsBasic")),
                                                         reader.GetBoolean(reader.GetOrdinal("IsDa")),
                                                         reader.GetBoolean(reader.GetOrdinal("IsOtBase")),
                                                         reader.GetDecimal(reader.GetOrdinal("Amount"))))
                        End While
                    End If
                    Return New EmployeeSalaryView(salaryId, effectiveFrom, lines)
                End Using
            End Using
        End Using
    End Function

    Public Function SaveSalary(employeeId As Integer, effectiveFrom As Date, reason As String, lines As IReadOnlyList(Of SalaryLineWrite),
                               userId As Integer?) As Integer Implements ISalaryRepository.SaveSalary
        Dim json As String = JsonSerializer.Serialize(lines.Select(Function(l) New With {.ComponentID = l.ComponentId, .Amount = l.Amount}).ToList())
        Return ExecuteScalarInt("pay.usp_Salary_Save",
                                Sub(cmd)
                                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                                    cmd.Parameters.Add("@EffectiveFrom", SqlDbType.Date).Value = effectiveFrom
                                    cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, MaxReasonLength).Value = StoredProcedure.DbValue(reason)
                                    cmd.Parameters.Add("@LinesJson", SqlDbType.NVarChar, -1).Value = json
                                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                                End Sub)
    End Function

    ''' <summary>A decimal parameter with its column precision and scale, so SqlClient does not round the value to a whole number.</summary>
    Private Shared Sub AddDecimal(cmd As SqlCommand, name As String, precision As Byte, scale As Byte, value As Decimal)
        Dim parameter As SqlParameter = cmd.Parameters.Add(name, SqlDbType.Decimal)
        parameter.Precision = precision
        parameter.Scale = scale
        parameter.Value = value
    End Sub

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
