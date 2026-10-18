Imports System.Data
Imports Revolution.Data

''' <summary>Read procedures of migration 0208. Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlAttendanceBoardRepository
    Implements IAttendanceBoardRepository

    Private Const CloseEventType As Integer = 1
    Private Const ReopenEventType As Integer = 2

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function ListDay(day As Date) As IReadOnlyList(Of AttendanceSheetRow) Implements IAttendanceBoardRepository.ListDay
        Dim rows As New List(Of AttendanceSheetRow)()
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "att.usp_Attendance_ListDay")
                cmd.Parameters.Add("@WorkDate", SqlDbType.Date).Value = day.Date
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim status As AttendanceStatus? = Nothing
                        If Not reader.IsDBNull(3) Then status = CType(CInt(reader.GetByte(3)), AttendanceStatus)
                        rows.Add(New AttendanceSheetRow(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), status,
                                                        StoredProcedure.ReadNullableInt(reader, "ShiftID"),
                                                        CInt(reader.GetInt16(5)), CInt(reader.GetInt16(6))))
                    End While
                End Using
            End Using
        End Using
        Return rows
    End Function

    Public Function ListMonthAll(year As Integer, month As Integer) As IReadOnlyList(Of MonthDayRow) Implements IAttendanceBoardRepository.ListMonthAll
        Dim rows As New List(Of MonthDayRow)()
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "att.usp_Attendance_ListMonthAll")
                cmd.Parameters.Add("@Year", SqlDbType.Int).Value = year
                cmd.Parameters.Add("@Month", SqlDbType.Int).Value = month
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim record As DayRecord = Nothing
                        If Not reader.IsDBNull(3) Then
                            record = New DayRecord(reader.GetDateTime(3), CType(CInt(reader.GetByte(4)), AttendanceStatus), CInt(reader.GetInt16(5)))
                        End If
                        rows.Add(New MonthDayRow(reader.GetInt32(0), reader.GetString(1), reader.GetString(2), record))
                    End While
                End Using
            End Using
        End Using
        Return rows
    End Function

    Public Function ListMonthEvents(monthKey As Integer) As IReadOnlyList(Of MonthLockEntry) Implements IAttendanceBoardRepository.ListMonthEvents
        Dim rows As New List(Of MonthLockEntry)()
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "att.usp_Month_ListEvents")
                cmd.Parameters.Add("@MonthKey", SqlDbType.Int).Value = monthKey
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim eventType As Integer = CInt(reader.GetByte(0))
                        rows.Add(New MonthLockEntry(eventType = ReopenEventType, StoredProcedure.ReadNullableInt(reader, "UserID"),
                                                    If(reader.IsDBNull(2), String.Empty, reader.GetString(2)), reader.GetDateTime(3)))
                    End While
                End Using
            End Using
        End Using
        Return rows
    End Function
End Class
