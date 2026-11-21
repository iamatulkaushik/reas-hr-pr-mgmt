Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Monthly attendance storage through att.usp_AttendanceMonthly_* and att.usp_AttendanceSetting_* (ADR 0001, ADR 0026). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlAttendanceMonthlyRepository
    Implements IAttendanceMonthlyRepository

    Private Const MaxRemarksLength As Integer = 100

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function ListMonth(periodStart As Date) As IReadOnlyList(Of MonthlyAttendanceRow) Implements IAttendanceMonthlyRepository.ListMonth
        Dim rows As New List(Of MonthlyAttendanceRow)()
        Try
            Using conn = _factory.Open()
                Using cmd = StoredProcedure.CreateCommand(conn, "att.usp_AttendanceMonthly_List")
                    cmd.Parameters.Add("@PeriodStart", SqlDbType.Date).Value = periodStart.Date
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            rows.Add(New MonthlyAttendanceRow(reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                                              reader.GetString(reader.GetOrdinal("EmpCode")),
                                                              reader.GetString(reader.GetOrdinal("FullName")),
                                                              reader.GetDateTime(reader.GetOrdinal("WindowFrom")),
                                                              reader.GetDateTime(reader.GetOrdinal("WindowTo")),
                                                              reader.GetInt32(reader.GetOrdinal("WindowDays")),
                                                              reader.GetBoolean(reader.GetOrdinal("HasRow")),
                                                              reader.GetInt32(reader.GetOrdinal("PresentDays")),
                                                              reader.GetInt32(reader.GetOrdinal("HalfDays")),
                                                              reader.GetInt32(reader.GetOrdinal("WeeklyOffDays")),
                                                              reader.GetInt32(reader.GetOrdinal("HolidayDays")),
                                                              reader.GetInt32(reader.GetOrdinal("PaidLeaveDays")),
                                                              reader.GetInt32(reader.GetOrdinal("AbsentDays")),
                                                              reader.GetDecimal(reader.GetOrdinal("OvertimeHours")),
                                                              If(reader.IsDBNull(reader.GetOrdinal("Remarks")), String.Empty, reader.GetString(reader.GetOrdinal("Remarks")))))
                        End While
                    End Using
                End Using
            End Using
        Catch ex As SqlException When AttendanceErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New AttendanceRuleException(AttendanceErrorMap.Translate(ex.Number, ex.Message), ex)
        End Try
        Return rows
    End Function

    Public Sub Save(periodStart As Date, row As MonthlyAttendanceRow, requireComplete As Boolean, userId As Integer?) Implements IAttendanceMonthlyRepository.Save
        If row Is Nothing Then Throw New ArgumentNullException(NameOf(row))
        Execute("att.usp_AttendanceMonthly_Save",
                Sub(cmd)
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = row.EmployeeId
                    cmd.Parameters.Add("@PeriodStart", SqlDbType.Date).Value = periodStart.Date
                    cmd.Parameters.Add("@PresentDays", SqlDbType.Int).Value = row.PresentDays
                    cmd.Parameters.Add("@HalfDays", SqlDbType.Int).Value = row.HalfDays
                    cmd.Parameters.Add("@WeeklyOffDays", SqlDbType.Int).Value = row.WeeklyOffDays
                    cmd.Parameters.Add("@HolidayDays", SqlDbType.Int).Value = row.HolidayDays
                    cmd.Parameters.Add("@PaidLeaveDays", SqlDbType.Int).Value = row.PaidLeaveDays
                    cmd.Parameters.Add("@AbsentDays", SqlDbType.Int).Value = row.AbsentDays
                    Dim hours = cmd.Parameters.Add("@OvertimeHours", SqlDbType.Decimal)
                    hours.Precision = 5
                    hours.Scale = 2
                    hours.Value = row.OvertimeHours
                    cmd.Parameters.Add("@Remarks", SqlDbType.NVarChar, MaxRemarksLength).Value = If(row.Remarks.Length = 0, DBNull.Value, CObj(row.Remarks))
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    cmd.Parameters.Add("@RequireComplete", SqlDbType.Bit).Value = requireComplete
                End Sub)
    End Sub

    Public Sub Delete(employeeId As Integer, periodStart As Date, userId As Integer?) Implements IAttendanceMonthlyRepository.Delete
        Execute("att.usp_AttendanceMonthly_Delete",
                Sub(cmd)
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                    cmd.Parameters.Add("@PeriodStart", SqlDbType.Date).Value = periodStart.Date
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Public Function GetMode() As AttendanceMode Implements IAttendanceMonthlyRepository.GetMode
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "att.usp_AttendanceSetting_Get")
                Using reader = cmd.ExecuteReader()
                    If Not reader.Read() Then Return AttendanceMode.Monthly
                    Return If(CInt(reader.GetByte(0)) = CInt(AttendanceMode.Daily), AttendanceMode.Daily, AttendanceMode.Monthly)
                End Using
            End Using
        End Using
    End Function

    Public Sub SetMode(mode As AttendanceMode) Implements IAttendanceMonthlyRepository.SetMode
        Execute("att.usp_AttendanceSetting_Set", Sub(cmd) cmd.Parameters.Add("@Mode", SqlDbType.TinyInt).Value = CByte(CInt(mode)))
    End Sub

    Private Sub Execute(procedure As String, bind As Action(Of SqlCommand))
        Try
            Using conn = _factory.Open()
                Using cmd = StoredProcedure.CreateCommand(conn, procedure)
                    bind(cmd)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When AttendanceErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New AttendanceRuleException(AttendanceErrorMap.Translate(ex.Number, ex.Message), ex)
        End Try
    End Sub
End Class
