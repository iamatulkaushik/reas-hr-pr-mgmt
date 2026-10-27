Imports System.Data
Imports System.Globalization
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Leave storage through pay.usp_LeaveType_*, usp_LeaveLedger_* and usp_LeaveBalance_List (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlLeaveRepository
    Implements ILeaveRepository

    Private Const MaxCodeLength As Integer = 10
    Private Const MaxNameLength As Integer = 100
    Private Const MaxNoteLength As Integer = 200

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function ListTypes() As IReadOnlyList(Of LeaveTypeItem) Implements ILeaveRepository.ListTypes
        Dim items As New List(Of LeaveTypeItem)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_LeaveType_List")
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New LeaveTypeItem(reader.GetInt32(reader.GetOrdinal("LeaveTypeID")), reader.GetString(reader.GetOrdinal("Code")),
                                                    reader.GetString(reader.GetOrdinal("Name")), reader.GetBoolean(reader.GetOrdinal("IsEncashable")),
                                                    ReadDays(reader, "MaxEncashDays"), ReadDays(reader, "MaxCarryForwardDays"),
                                                    reader.GetBoolean(reader.GetOrdinal("IsActive"))))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Function SaveType(leaveType As LeaveTypeItem) As Integer Implements ILeaveRepository.SaveType
        Return ExecuteScalarInt("pay.usp_LeaveType_Save",
                                Sub(cmd)
                                    cmd.Parameters.Add("@LeaveTypeID", SqlDbType.Int).Value = StoredProcedure.DbValue(leaveType.LeaveTypeId)
                                    cmd.Parameters.Add("@Code", SqlDbType.NVarChar, MaxCodeLength).Value = leaveType.Code
                                    cmd.Parameters.Add("@Name", SqlDbType.NVarChar, MaxNameLength).Value = leaveType.Name
                                    cmd.Parameters.Add("@IsEncashable", SqlDbType.Bit).Value = leaveType.IsEncashable
                                    AddNullableDays(cmd, "@MaxEncashDays", leaveType.MaxEncashDays)
                                    AddNullableDays(cmd, "@MaxCarryForwardDays", leaveType.MaxCarryForwardDays)
                                    cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = leaveType.IsActive
                                End Sub)
    End Function

    Public Function Post(employeeId As Integer, leaveTypeId As Integer, leaveYear As Integer, entryDate As Date, kind As LeaveEntryKind, days As Decimal,
                         note As String, userId As Integer?) As Long Implements ILeaveRepository.Post
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_LeaveLedger_Post")
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                    cmd.Parameters.Add("@LeaveTypeID", SqlDbType.Int).Value = leaveTypeId
                    cmd.Parameters.Add("@LeaveYear", SqlDbType.SmallInt).Value = CShort(leaveYear)
                    cmd.Parameters.Add("@EntryDate", SqlDbType.Date).Value = entryDate.Date
                    cmd.Parameters.Add("@EntryTypeID", SqlDbType.TinyInt).Value = CByte(kind)
                    AddDays(cmd, "@Days", days)
                    cmd.Parameters.Add("@Note", SqlDbType.NVarChar, MaxNoteLength).Value = StoredProcedure.DbValue(If(String.IsNullOrEmpty(note), Nothing, note))
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    Return Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture)
                End Using
            End Using
        Catch ex As SqlException When PayrollErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New PayrollRuleException(PayrollErrorMap.Translate(ex.Number, ex.Message), ex.Number, ex)
        End Try
    End Function

    Public Function ListBalances(employeeId As Integer) As IReadOnlyList(Of LeaveBalanceRow) Implements ILeaveRepository.ListBalances
        Dim items As New List(Of LeaveBalanceRow)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_LeaveBalance_List")
                cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New LeaveBalanceRow(reader.GetInt32(reader.GetOrdinal("LeaveTypeID")), reader.GetString(reader.GetOrdinal("Code")),
                                                      reader.GetString(reader.GetOrdinal("Name")), reader.GetBoolean(reader.GetOrdinal("IsEncashable")),
                                                      ReadDays(reader, "MaxEncashDays"), reader.GetDecimal(reader.GetOrdinal("AccruedDays")),
                                                      reader.GetDecimal(reader.GetOrdinal("CarriedForwardDays")), reader.GetDecimal(reader.GetOrdinal("TakenDays")),
                                                      reader.GetDecimal(reader.GetOrdinal("EncashedDays")), reader.GetDecimal(reader.GetOrdinal("LapsedDays")),
                                                      reader.GetDecimal(reader.GetOrdinal("AdjustedDays")), reader.GetDecimal(reader.GetOrdinal("BalanceDays"))))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Function ListEntries(employeeId As Integer, leaveTypeId As Integer?) As IReadOnlyList(Of LeaveEntryView) Implements ILeaveRepository.ListEntries
        Dim items As New List(Of LeaveEntryView)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "pay.usp_LeaveLedger_List")
                cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                cmd.Parameters.Add("@LeaveTypeID", SqlDbType.Int).Value = StoredProcedure.DbValue(leaveTypeId)
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        Dim noteOrdinal As Integer = reader.GetOrdinal("Note")
                        items.Add(New LeaveEntryView(reader.GetInt64(reader.GetOrdinal("LedgerID")), reader.GetInt32(reader.GetOrdinal("LeaveTypeID")),
                                                     CInt(reader.GetInt16(reader.GetOrdinal("LeaveYear"))), reader.GetDateTime(reader.GetOrdinal("EntryDate")),
                                                     CType(CInt(reader.GetByte(reader.GetOrdinal("EntryTypeID"))), LeaveEntryKind),
                                                     reader.GetDecimal(reader.GetOrdinal("Days")), If(reader.IsDBNull(noteOrdinal), String.Empty, reader.GetString(noteOrdinal))))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Private Shared Function ReadDays(reader As SqlDataReader, column As String) As Decimal?
        Dim ordinal As Integer = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(ordinal), CType(Nothing, Decimal?), reader.GetDecimal(ordinal))
    End Function

    ''' <summary>A days parameter with the column precision and scale, so SqlClient does not round to a whole number.</summary>
    Private Shared Sub AddDays(cmd As SqlCommand, name As String, value As Decimal)
        Dim parameter As SqlParameter = cmd.Parameters.Add(name, SqlDbType.Decimal)
        parameter.Precision = 6
        parameter.Scale = 2
        parameter.Value = value
    End Sub

    Private Shared Sub AddNullableDays(cmd As SqlCommand, name As String, value As Decimal?)
        Dim parameter As SqlParameter = cmd.Parameters.Add(name, SqlDbType.Decimal)
        parameter.Precision = 6
        parameter.Scale = 2
        parameter.Value = If(value.HasValue, CObj(value.Value), DBNull.Value)
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
