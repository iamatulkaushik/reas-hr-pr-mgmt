Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Roster storage through att.* stored procedures (ADR 0008).</summary>
Public NotInheritable Class SqlRosterRepository
    Implements IRosterRepository

    Private Const NameLength As Integer = 100

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function ListShifts() As IReadOnlyList(Of ShiftItem) Implements IRosterRepository.ListShifts
        Dim items As New List(Of ShiftItem)()
        Read("att.usp_Shift_List", Nothing,
             Sub(r)
                 items.Add(New ShiftItem(r.GetInt32(0), r.GetString(1), r.GetTimeSpan(2), r.GetTimeSpan(3), CInt(r.GetInt16(4)),
                                         CInt(r.GetInt16(5)), CInt(r.GetInt16(6)), CInt(r.GetInt16(7)),
                                         CType(CInt(r.GetByte(8)), OvertimeRounding), r.GetBoolean(9)))
             End Sub)
        Return items
    End Function

    Public Function SaveShift(item As ShiftItem) As Integer Implements IRosterRepository.SaveShift
        Dim id As Integer
        Read("att.usp_Shift_Save",
             Sub(cmd)
                 cmd.Parameters.Add("@ShiftID", SqlDbType.Int).Value = StoredProcedure.DbValue(item.ShiftId)
                 cmd.Parameters.Add("@ShiftName", SqlDbType.NVarChar, NameLength).Value = item.Name
                 cmd.Parameters.Add("@StartTime", SqlDbType.Time).Value = item.StartTime
                 cmd.Parameters.Add("@EndTime", SqlDbType.Time).Value = item.EndTime
                 cmd.Parameters.Add("@UnpaidBreakMinutes", SqlDbType.SmallInt).Value = CShort(item.UnpaidBreakMinutes)
                 cmd.Parameters.Add("@BreakAppliesAfterMinutes", SqlDbType.SmallInt).Value = CShort(item.BreakAppliesAfterMinutes)
                 cmd.Parameters.Add("@FullDayMinutes", SqlDbType.SmallInt).Value = CShort(item.FullDayMinutes)
                 cmd.Parameters.Add("@HalfDayMinutes", SqlDbType.SmallInt).Value = CShort(item.HalfDayMinutes)
                 cmd.Parameters.Add("@OvertimeStepMinutes", SqlDbType.TinyInt).Value = CByte(CInt(item.OvertimeStep))
                 cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = item.IsActive
             End Sub,
             Sub(r) id = r.GetInt32(0))
        Return id
    End Function

    Public Function ListHolidays(year As Integer) As IReadOnlyList(Of HolidayItem) Implements IRosterRepository.ListHolidays
        Dim items As New List(Of HolidayItem)()
        Read("att.usp_Holiday_List", Sub(cmd) cmd.Parameters.Add("@Year", SqlDbType.Int).Value = year,
             Sub(r) items.Add(New HolidayItem(r.GetDateTime(1), r.GetString(2))))
        Return items
    End Function

    Public Sub SaveHoliday(item As HolidayItem) Implements IRosterRepository.SaveHoliday
        Read("att.usp_Holiday_Save",
             Sub(cmd)
                 cmd.Parameters.Add("@HolidayDate", SqlDbType.Date).Value = item.Day
                 cmd.Parameters.Add("@HolidayName", SqlDbType.NVarChar, NameLength).Value = item.Name
             End Sub, Nothing)
    End Sub

    Public Sub DeleteHoliday(day As Date) Implements IRosterRepository.DeleteHoliday
        Read("att.usp_Holiday_Delete", Sub(cmd) cmd.Parameters.Add("@HolidayDate", SqlDbType.Date).Value = day.Date, Nothing)
    End Sub

    Public Function ListWeeklyOff() As IReadOnlyList(Of WeeklyOffRule) Implements IRosterRepository.ListWeeklyOff
        Dim items As New List(Of WeeklyOffRule)()
        Read("att.usp_WeeklyOff_List", Nothing, Sub(r) items.Add(New WeeklyOffRule(r.GetDateTime(0), CInt(r.GetByte(1)))))
        Return items
    End Function

    Public Sub SaveWeeklyOff(rule As WeeklyOffRule) Implements IRosterRepository.SaveWeeklyOff
        Read("att.usp_WeeklyOff_Save",
             Sub(cmd)
                 cmd.Parameters.Add("@EffectiveFrom", SqlDbType.Date).Value = rule.EffectiveFrom
                 cmd.Parameters.Add("@DaysMask", SqlDbType.TinyInt).Value = CByte(rule.DaysMask)
             End Sub, Nothing)
    End Sub

    Public Function ListEmployeeShifts(employeeId As Integer) As IReadOnlyList(Of EmployeeShiftItem) Implements IRosterRepository.ListEmployeeShifts
        Dim items As New List(Of EmployeeShiftItem)()
        Read("att.usp_EmployeeShift_List", Sub(cmd) cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId,
             Sub(r)
                 Dim effectiveTo As Date? = Nothing
                 If Not r.IsDBNull(3) Then effectiveTo = r.GetDateTime(3)
                 items.Add(New EmployeeShiftItem(r.GetInt32(0), r.GetString(1), r.GetDateTime(2), effectiveTo))
             End Sub)
        Return items
    End Function

    Public Sub AssignShift(employeeId As Integer, shiftId As Integer, effectiveFrom As Date) Implements IRosterRepository.AssignShift
        Read("att.usp_EmployeeShift_Assign",
             Sub(cmd)
                 cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                 cmd.Parameters.Add("@ShiftID", SqlDbType.Int).Value = shiftId
                 cmd.Parameters.Add("@EffectiveFrom", SqlDbType.Date).Value = effectiveFrom.Date
             End Sub, Nothing)
    End Sub

    Private Sub Read(procedure As String, bind As Action(Of SqlCommand), onRow As Action(Of SqlDataReader))
        Try
            Using conn = _factory.Open()
                Using cmd = StoredProcedure.CreateCommand(conn, procedure)
                    If bind IsNot Nothing Then bind(cmd)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            If onRow IsNot Nothing Then onRow(reader)
                        End While
                    End Using
                End Using
            End Using
        Catch ex As SqlException When RosterErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New AttendanceRuleException(RosterErrorMap.Translate(ex.Number, ex.Message), ex)
        End Try
    End Sub
End Class
