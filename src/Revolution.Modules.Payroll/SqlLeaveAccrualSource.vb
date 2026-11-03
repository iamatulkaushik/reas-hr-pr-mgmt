Imports System.Data
Imports System.Globalization
Imports System.Text.Json
Imports Microsoft.Data.SqlClient
Imports Revolution.Core
Imports Revolution.Data

''' <summary>Leave accrual storage through pay.usp_LeaveAccrualSetting_*, usp_LeaveAccrual_Present and usp_LeaveAccrual_Post (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlLeaveAccrualSource
    Implements ILeaveAccrualSource

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function ListSettings() As IReadOnlyList(Of LeaveAccrualSettingItem) Implements ILeaveAccrualSource.ListSettings
        Dim items As New List(Of LeaveAccrualSettingItem)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_LeaveAccrualSetting_List")
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New LeaveAccrualSettingItem(reader.GetInt32(reader.GetOrdinal("LeaveTypeID")), reader.GetString(reader.GetOrdinal("Code")),
                                                              reader.GetString(reader.GetOrdinal("Name")), ReadSetting(reader)))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Sub SaveSetting(leaveTypeId As Integer, setting As LeaveAccrualSetting, userId As Integer?) Implements ILeaveAccrualSource.SaveSetting
        If setting Is Nothing Then Throw New ArgumentNullException(NameOf(setting))
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_LeaveAccrualSetting_Save")
                    cmd.Parameters.Add("@LeaveTypeID", SqlDbType.Int).Value = leaveTypeId
                    cmd.Parameters.Add("@EarnedDays", SqlDbType.Decimal).Value = setting.EarnedDays
                    cmd.Parameters.Add("@PerWorkedDays", SqlDbType.Decimal).Value = setting.PerWorkedDays
                    cmd.Parameters.Add("@RoundingID", SqlDbType.TinyInt).Value = CByte(setting.Rounding)
                    cmd.Parameters.Add("@MaxBalance", SqlDbType.Decimal).Value = StoredProcedure.DbValue(setting.MaxBalance)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When LeaveAccrualErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New PayrollRuleException(LeaveAccrualErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
    End Sub

    Public Function ListPresent(leaveTypeId As Integer, month As Date) As IReadOnlyList(Of LeaveAccrualInput) Implements ILeaveAccrualSource.ListPresent
        Dim rows As New List(Of LeaveAccrualInput)()
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_LeaveAccrual_Present")
                    cmd.Parameters.Add("@LeaveTypeID", SqlDbType.Int).Value = leaveTypeId
                    cmd.Parameters.Add("@AccrualMonth", SqlDbType.Date).Value = month.Date
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            rows.Add(New LeaveAccrualInput(reader.GetInt32(reader.GetOrdinal("EmployeeID")), reader.GetDecimal(reader.GetOrdinal("PresentDays")),
                                                           reader.GetDecimal(reader.GetOrdinal("Balance"))))
                        End While
                    End Using
                End Using
            End Using
        Catch ex As SqlException When LeaveAccrualErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New PayrollRuleException(LeaveAccrualErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
        Return rows
    End Function

    Public Function Post(leaveTypeId As Integer, month As Date, entries As IReadOnlyList(Of LeaveAccrualEntry), userId As Integer?) As Integer Implements ILeaveAccrualSource.Post
        If entries Is Nothing Then Throw New ArgumentNullException(NameOf(entries))
        Dim items As New List(Of Dictionary(Of String, Object))()
        For Each entry As LeaveAccrualEntry In entries
            items.Add(New Dictionary(Of String, Object) From {{"employeeId", entry.EmployeeId}, {"days", entry.Days}})
        Next
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_LeaveAccrual_Post")
                    cmd.Parameters.Add("@LeaveTypeID", SqlDbType.Int).Value = leaveTypeId
                    cmd.Parameters.Add("@AccrualMonth", SqlDbType.Date).Value = month.Date
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    cmd.Parameters.Add("@EntriesJson", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(items)
                    Return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture)
                End Using
            End Using
        Catch ex As SqlException When LeaveAccrualErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New PayrollRuleException(LeaveAccrualErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
    End Function

    Private Shared Function ReadSetting(reader As SqlDataReader) As LeaveAccrualSetting
        Dim earned As Integer = reader.GetOrdinal("EarnedDays")
        If reader.IsDBNull(earned) Then Return Nothing
        Dim cap As Integer = reader.GetOrdinal("MaxBalance")
        Return New LeaveAccrualSetting(reader.GetDecimal(earned), reader.GetDecimal(reader.GetOrdinal("PerWorkedDays")),
                                       CType(CInt(reader.GetByte(reader.GetOrdinal("RoundingID"))), RateRounding),
                                       If(reader.IsDBNull(cap), CType(Nothing, Decimal?), reader.GetDecimal(cap)))
    End Function
End Class
