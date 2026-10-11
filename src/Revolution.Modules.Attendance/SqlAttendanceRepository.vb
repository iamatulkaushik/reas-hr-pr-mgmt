Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Attendance storage through att.* stored procedures (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlAttendanceRepository
    Implements IAttendanceRepository

    Private Const ErrorMonthClosed As Integer = 50001
    Private Const ErrorNoCompany As Integer = 50002
    Private Const ErrorAlreadyClosed As Integer = 50003
    Private Const ErrorNotClosed As Integer = 50004
    Private Const ErrorReasonShort As Integer = 50005
    Private Const MaxReasonLength As Integer = 500

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function ListLockedMonths() As ISet(Of Integer) Implements IAttendanceRepository.ListLockedMonths
        Dim keys As New HashSet(Of Integer)()
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "att.usp_Month_ListLocked")
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        keys.Add(reader.GetInt32(0))
                    End While
                End Using
            End Using
        End Using
        Return keys
    End Function

    Public Function ListMonth(employeeId As Integer, year As Integer, month As Integer) As IReadOnlyList(Of DayRecord) Implements IAttendanceRepository.ListMonth
        Dim days As New List(Of DayRecord)()
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "att.usp_Attendance_ListMonth")
                cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                cmd.Parameters.Add("@Year", SqlDbType.Int).Value = year
                cmd.Parameters.Add("@Month", SqlDbType.Int).Value = month
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        days.Add(New DayRecord(reader.GetDateTime(0), CType(CInt(reader.GetByte(1)), AttendanceStatus), CInt(reader.GetInt16(2))))
                    End While
                End Using
            End Using
        End Using
        Return days
    End Function

    Public Sub SaveDay(employeeId As Integer, record As DayRecord, shiftId As Integer?, workedMinutes As Integer, userId As Integer?) Implements IAttendanceRepository.SaveDay
        Execute("att.usp_Attendance_Save",
                Sub(cmd)
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                    cmd.Parameters.Add("@WorkDate", SqlDbType.Date).Value = record.Day
                    cmd.Parameters.Add("@StatusID", SqlDbType.TinyInt).Value = CByte(CInt(record.Status))
                    cmd.Parameters.Add("@ShiftID", SqlDbType.Int).Value = StoredProcedure.DbValue(shiftId)
                    cmd.Parameters.Add("@WorkedMinutes", SqlDbType.SmallInt).Value = CShort(workedMinutes)
                    cmd.Parameters.Add("@OvertimeMinutes", SqlDbType.SmallInt).Value = CShort(record.OvertimeMinutes)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Public Sub CloseMonth(monthKey As Integer, userId As Integer?) Implements IAttendanceRepository.CloseMonth
        Execute("att.usp_Month_Close",
                Sub(cmd)
                    cmd.Parameters.Add("@MonthKey", SqlDbType.Int).Value = monthKey
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Public Sub ReopenMonth(monthKey As Integer, reason As String, userId As Integer?) Implements IAttendanceRepository.ReopenMonth
        Execute("att.usp_Month_Reopen",
                Sub(cmd)
                    cmd.Parameters.Add("@MonthKey", SqlDbType.Int).Value = monthKey
                    cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, MaxReasonLength).Value = reason
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Private Sub Execute(procedure As String, bind As Action(Of SqlCommand))
        Try
            Using conn = _factory.Open()
                Using cmd = StoredProcedure.CreateCommand(conn, procedure)
                    bind(cmd)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When IsRuleError(ex.Number)
            Throw New AttendanceRuleException(ex.Message, ex)
        End Try
    End Sub

    Private Shared Function IsRuleError(number As Integer) As Boolean
        Return number = ErrorMonthClosed OrElse number = ErrorNoCompany OrElse number = ErrorAlreadyClosed OrElse number = ErrorNotClosed OrElse number = ErrorReasonShort
    End Function
End Class
