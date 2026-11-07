Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Biometric storage through att.usp_Device_*, usp_RfidCard_*, usp_Punch_List and usp_Punch_Resolve (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlBiometricRepository
    Implements IBiometricRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function ListDevices(includeInactive As Boolean) As IReadOnlyList(Of DeviceItem) Implements IBiometricRepository.ListDevices
        Dim items As New List(Of DeviceItem)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "att.usp_Device_List")
                cmd.Parameters.Add("@IncludeInactive", SqlDbType.Bit).Value = includeInactive
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New DeviceItem(reader.GetInt32(reader.GetOrdinal("DeviceID")), reader.GetInt32(reader.GetOrdinal("FactoryID")),
                                                 reader.GetString(reader.GetOrdinal("FactoryName")), reader.GetString(reader.GetOrdinal("DeviceCode")),
                                                 reader.GetString(reader.GetOrdinal("DeviceName")), reader.GetString(reader.GetOrdinal("Brand")),
                                                 reader.GetString(reader.GetOrdinal("Host")), reader.GetInt32(reader.GetOrdinal("Port")),
                                                 reader.GetBoolean(reader.GetOrdinal("IsActive")), NullableDate(reader, "LastPunchTime"), NullableDate(reader, "LastPulledUtc")))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Function SaveDevice(device As DeviceWrite, userId As Integer?) As Integer Implements IBiometricRepository.SaveDevice
        If device Is Nothing Then Throw New ArgumentNullException(NameOf(device))
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "att.usp_Device_Save")
                    cmd.Parameters.Add("@DeviceID", SqlDbType.Int).Value = StoredProcedure.DbValue(device.DeviceId)
                    cmd.Parameters.Add("@FactoryID", SqlDbType.Int).Value = device.FactoryId
                    cmd.Parameters.Add("@DeviceCode", SqlDbType.NVarChar, 30).Value = device.Code
                    cmd.Parameters.Add("@DeviceName", SqlDbType.NVarChar, 100).Value = device.Name
                    cmd.Parameters.Add("@Brand", SqlDbType.NVarChar, 30).Value = device.Brand
                    cmd.Parameters.Add("@Host", SqlDbType.NVarChar, 100).Value = device.Host
                    cmd.Parameters.Add("@Port", SqlDbType.Int).Value = device.Port
                    cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = device.IsActive
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    Return Convert.ToInt32(cmd.ExecuteScalar(), Globalization.CultureInfo.InvariantCulture)
                End Using
            End Using
        Catch ex As SqlException When BiometricErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New BiometricRuleException(BiometricErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
    End Function

    Public Function ListCards(employeeId As Integer?, includeRetired As Boolean) As IReadOnlyList(Of RfidCardItem) Implements IBiometricRepository.ListCards
        Dim items As New List(Of RfidCardItem)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "att.usp_RfidCard_List")
                cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = StoredProcedure.DbValue(employeeId)
                cmd.Parameters.Add("@IncludeRetired", SqlDbType.Bit).Value = includeRetired
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New RfidCardItem(reader.GetInt32(reader.GetOrdinal("CardID")), reader.GetInt32(reader.GetOrdinal("EmployeeID")),
                                                   reader.GetString(reader.GetOrdinal("EmpCode")), reader.GetString(reader.GetOrdinal("FullName")),
                                                   reader.GetString(reader.GetOrdinal("CardNo")), reader.GetBoolean(reader.GetOrdinal("IsActive")),
                                                   reader.GetDateTime(reader.GetOrdinal("MappedUtc")), NullableDate(reader, "RetiredUtc")))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Sub MapCard(employeeId As Integer, cardNo As String, userId As Integer?) Implements IBiometricRepository.MapCard
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "att.usp_RfidCard_Map")
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                    cmd.Parameters.Add("@CardNo", SqlDbType.NVarChar, 30).Value = cardNo
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When BiometricErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New BiometricRuleException(BiometricErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
    End Sub

    Public Sub UnmapCard(employeeId As Integer, userId As Integer?) Implements IBiometricRepository.UnmapCard
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "att.usp_RfidCard_Unmap")
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When BiometricErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New BiometricRuleException(BiometricErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
    End Sub

    Public Function ListPunches(filter As PunchFilter) As IReadOnlyList(Of PunchRow) Implements IBiometricRepository.ListPunches
        If filter Is Nothing Then Throw New ArgumentNullException(NameOf(filter))
        Dim rows As New List(Of PunchRow)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "att.usp_Punch_List")
                cmd.Parameters.Add("@FromDate", SqlDbType.Date).Value = filter.FromDate.Date
                cmd.Parameters.Add("@ToDate", SqlDbType.Date).Value = filter.ToDate.Date
                cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = StoredProcedure.DbValue(filter.EmployeeId)
                cmd.Parameters.Add("@DeviceID", SqlDbType.Int).Value = StoredProcedure.DbValue(filter.DeviceId)
                cmd.Parameters.Add("@UnmatchedOnly", SqlDbType.Bit).Value = filter.UnmatchedOnly
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        rows.Add(New PunchRow(reader.GetInt64(reader.GetOrdinal("PunchID")), reader.GetInt32(reader.GetOrdinal("DeviceID")), reader.GetString(reader.GetOrdinal("DeviceCode")),
                                              reader.GetInt32(reader.GetOrdinal("FactoryID")), StoredProcedure.ReadNullableInt(reader, "DeviceUserID"), NullableString(reader, "CardNo"),
                                              reader.GetDateTime(reader.GetOrdinal("PunchTime")), NullableByte(reader, "VerifyMode"), NullableByte(reader, "DeviceStatus"),
                                              StoredProcedure.ReadNullableInt(reader, "EmployeeID"), NullableString(reader, "EmpCode"), NullableString(reader, "FullName")))
                    End While
                End Using
            End Using
        End Using
        Return rows
    End Function

    Public Function ResolvePunches() As ResolveResult Implements IBiometricRepository.ResolvePunches
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "att.usp_Punch_Resolve")
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    If Not reader.Read() Then Return New ResolveResult(0, 0)
                    Return New ResolveResult(reader.GetInt32(reader.GetOrdinal("Resolved")), reader.GetInt32(reader.GetOrdinal("StillUnmatched")))
                End Using
            End Using
        End Using
    End Function

    Public Function ListUsers(onlyUnassigned As Boolean) As IReadOnlyList(Of BiometricUserItem) Implements IBiometricRepository.ListUsers
        Dim items As New List(Of BiometricUserItem)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "att.usp_BiometricUser_List")
                cmd.Parameters.Add("@OnlyUnassigned", SqlDbType.Bit).Value = onlyUnassigned
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New BiometricUserItem(reader.GetInt32(reader.GetOrdinal("EmployeeID")), reader.GetString(reader.GetOrdinal("EmpCode")),
                                                        reader.GetString(reader.GetOrdinal("FullName")), StoredProcedure.ReadNullableInt(reader, "BiometricID"),
                                                        NullableString(reader, "CardNo"), NullableDate(reader, "LastPunchTime"), reader.GetInt32(reader.GetOrdinal("Punches30"))))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Function ListUnknownUsers() As IReadOnlyList(Of UnknownBiometricUser) Implements IBiometricRepository.ListUnknownUsers
        Dim items As New List(Of UnknownBiometricUser)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "att.usp_BiometricUser_Unknown")
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New UnknownBiometricUser(StoredProcedure.ReadNullableInt(reader, "DeviceUserID"), NullableString(reader, "CardNo"),
                                                           reader.GetInt32(reader.GetOrdinal("PunchCount")), reader.GetDateTime(reader.GetOrdinal("FirstSeen")),
                                                           reader.GetDateTime(reader.GetOrdinal("LastSeen")), reader.GetInt32(reader.GetOrdinal("DeviceCount"))))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function

    Public Function NextBiometricId() As Integer Implements IBiometricRepository.NextBiometricId
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "att.usp_BiometricUser_NextId")
                Return Convert.ToInt32(cmd.ExecuteScalar(), Globalization.CultureInfo.InvariantCulture)
            End Using
        End Using
    End Function

    Public Sub AssignBiometricId(employeeId As Integer, biometricId As Integer?, userId As Integer?) Implements IBiometricRepository.AssignBiometricId
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "att.usp_BiometricUser_Assign")
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                    cmd.Parameters.Add("@BiometricID", SqlDbType.Int).Value = StoredProcedure.DbValue(biometricId)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As SqlException When BiometricErrorMap.Translate(ex.Number) IsNot Nothing
            Throw New BiometricRuleException(BiometricErrorMap.Translate(ex.Number), ex.Number, ex)
        End Try
    End Sub

    Public Function ListPunchTimes(fromTime As Date, toTime As Date) As IReadOnlyList(Of EmployeePunchTime) Implements IBiometricRepository.ListPunchTimes
        Dim rows As New List(Of EmployeePunchTime)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "att.usp_Punch_Times")
                cmd.Parameters.Add("@FromTime", SqlDbType.DateTime2).Value = fromTime
                cmd.Parameters.Add("@ToTime", SqlDbType.DateTime2).Value = toTime
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        rows.Add(New EmployeePunchTime(reader.GetInt32(reader.GetOrdinal("EmployeeID")), reader.GetDateTime(reader.GetOrdinal("PunchTime"))))
                    End While
                End Using
            End Using
        End Using
        Return rows
    End Function

    Private Shared Function NullableDate(reader As SqlDataReader, name As String) As Date?
        Dim i As Integer = reader.GetOrdinal(name)
        Return If(reader.IsDBNull(i), CType(Nothing, Date?), reader.GetDateTime(i))
    End Function

    Private Shared Function NullableByte(reader As SqlDataReader, name As String) As Byte?
        Dim i As Integer = reader.GetOrdinal(name)
        Return If(reader.IsDBNull(i), CType(Nothing, Byte?), reader.GetByte(i))
    End Function

    Private Shared Function NullableString(reader As SqlDataReader, name As String) As String
        Dim i As Integer = reader.GetOrdinal(name)
        Return If(reader.IsDBNull(i), Nothing, reader.GetString(i))
    End Function
End Class
