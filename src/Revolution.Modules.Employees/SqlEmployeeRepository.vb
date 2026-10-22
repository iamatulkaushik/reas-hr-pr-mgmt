Imports System.Data
Imports System.Text.Json
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Employee storage through mst.* stored procedures (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlEmployeeRepository
    Implements IEmployeeRepository

    Private Const MaxListRows As Integer = 500
    Private Const DefaultNationality As String = "Indian"

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function Create(draft As EmployeeDraft, placement As EmployeePlacement, userId As Integer?) As NewEmployee Implements IEmployeeRepository.Create
        Dim created As NewEmployee = Nothing
        EmployeeSql.Run(Sub()
                Using conn = _factory.Open()
                    Using cmd = StoredProcedure.CreateCommand(conn, "mst.usp_Employee_Create")
                        cmd.Parameters.Add("@EmpCode", SqlDbType.NVarChar, 20).Value = DBNull.Value
                        BindFields(cmd, draft, placement, userId)
                        Using reader = cmd.ExecuteReader()
                            If reader.Read() Then
                                created = New NewEmployee(reader.GetInt32(0), reader.GetString(1))
                            End If
                        End Using
                    End Using
                End Using
            End Sub)
        If created Is Nothing Then Throw New InvalidOperationException("The employee procedure returned no row.")
        Return created
    End Function

    Public Sub Update(employeeId As Integer, rowVersion As Byte(), draft As EmployeeDraft, placement As EmployeePlacement, userId As Integer?) Implements IEmployeeRepository.Update
        Execute("mst.usp_Employee_Update",
                Sub(cmd)
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                    cmd.Parameters.Add("@RowVer", SqlDbType.Binary, 8).Value = If(rowVersion Is Nothing, CObj(DBNull.Value), CObj(rowVersion))
                    BindFields(cmd, draft, placement, userId)
                End Sub)
    End Sub

    Public Function GetById(employeeId As Integer) As EmployeeDetail Implements IEmployeeRepository.GetById
        Dim found As EmployeeDetail = Nothing
        EmployeeSql.Run(Sub()
                Using conn = _factory.Open()
                    Using cmd = StoredProcedure.CreateCommand(conn, "mst.usp_Employee_GetById")
                        cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                        Using reader = cmd.ExecuteReader()
                            If reader.Read() Then
                                found = ReadDetail(reader)
                            End If
                        End Using
                    End Using
                End Using
            End Sub)
        Return found
    End Function

    Public Function List(search As String, factoryId As Integer?, activeOnly As Boolean) As IReadOnlyList(Of EmployeeSummary) Implements IEmployeeRepository.List
        Dim rows As New List(Of EmployeeSummary)()
        EmployeeSql.Run(Sub()
                Using conn = _factory.Open()
                    Using cmd = StoredProcedure.CreateCommand(conn, "mst.usp_Employee_List")
                        cmd.Parameters.Add("@Search", SqlDbType.NVarChar, 100).Value = If(String.IsNullOrWhiteSpace(search), CObj(DBNull.Value), search.Trim())
                        cmd.Parameters.Add("@FactoryID", SqlDbType.Int).Value = StoredProcedure.DbValue(factoryId)
                        cmd.Parameters.Add("@ActiveOnly", SqlDbType.Bit).Value = activeOnly
                        cmd.Parameters.Add("@Top", SqlDbType.Int).Value = MaxListRows
                        Using reader = cmd.ExecuteReader()
                            While reader.Read()
                                rows.Add(ReadSummary(reader))
                            End While
                        End Using
                    End Using
                End Using
            End Sub)
        Return rows
    End Function

    Public Sub RecordExit(employeeId As Integer, exitDate As Date, status As EmployeeStatus, userId As Integer?) Implements IEmployeeRepository.RecordExit
        Execute("mst.usp_Employee_Exit",
                Sub(cmd)
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                    cmd.Parameters.Add("@ExitDate", SqlDbType.Date).Value = exitDate.Date
                    cmd.Parameters.Add("@StatusID", SqlDbType.TinyInt).Value = CByte(CInt(status))
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Public Sub Reactivate(employeeId As Integer, userId As Integer?) Implements IEmployeeRepository.Reactivate
        Execute("mst.usp_Employee_Reactivate",
                Sub(cmd)
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Public Function GetIdentifierLast4(employeeId As Integer) As IdentifierLast4 Implements IEmployeeRepository.GetIdentifierLast4
        Dim found As IdentifierLast4 = Nothing
        EmployeeSql.Run(Sub()
                Using conn = _factory.Open()
                    Using cmd = StoredProcedure.CreateCommand(conn, "mst.usp_EmployeeStatutory_Get")
                        cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                        Using reader = cmd.ExecuteReader()
                            If reader.Read() Then
                                found = New IdentifierLast4 With {
                                    .Uan = EmployeeSql.OptText(reader, "UanLast4"), .EsiIp = EmployeeSql.OptText(reader, "EsiIpLast4"),
                                    .Pan = EmployeeSql.OptText(reader, "PanLast4"), .Aadhaar = EmployeeSql.OptText(reader, "AadhaarLast4")}
                            End If
                        End Using
                    End Using
                End Using
            End Sub)
        Return found
    End Function

    Public Function GetEncryptedIdentifiers(employeeId As Integer) As EncryptedIdentifiers Implements IEmployeeRepository.GetEncryptedIdentifiers
        Dim found As EncryptedIdentifiers = Nothing
        EmployeeSql.Run(Sub()
                Using conn = _factory.Open()
                    Using cmd = StoredProcedure.CreateCommand(conn, "mst.usp_EmployeeStatutory_GetEncrypted")
                        cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                        Using reader = cmd.ExecuteReader()
                            If reader.Read() Then
                                found = New EncryptedIdentifiers(reader.GetByte(reader.GetOrdinal("IdKeyVersion")),
                                                                 EmployeeSql.OptBytes(reader, "UanEnc"), EmployeeSql.OptBytes(reader, "EsiIpEnc"),
                                                                 EmployeeSql.OptBytes(reader, "PanEnc"), EmployeeSql.OptBytes(reader, "AadhaarEnc"))
                            End If
                        End Using
                    End Using
                End Using
            End Sub)
        Return found
    End Function

    Public Sub SetIdentifiers(employeeId As Integer, write As IdentifierWrite, userId As Integer?) Implements IEmployeeRepository.SetIdentifiers
        If write Is Nothing Then Throw New ArgumentNullException(NameOf(write))
        Execute("mst.usp_EmployeeStatutory_SetIdentifiers",
                Sub(cmd)
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                    cmd.Parameters.Add("@IdKeyVersion", SqlDbType.TinyInt).Value = write.KeyVersion
                    BindId(cmd, "Uan", write.Uan)
                    BindId(cmd, "EsiIp", write.EsiIp)
                    BindId(cmd, "Pan", write.Pan)
                    BindId(cmd, "Aadhaar", write.Aadhaar)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
                End Sub)
    End Sub

    Public Sub LogReveal(userId As Integer, employeeId As Integer, fields As String) Implements IEmployeeRepository.LogReveal
        Execute("aud.usp_Reveal_Insert",
                Sub(cmd)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = userId
                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                    cmd.Parameters.Add("@Fields", SqlDbType.NVarChar, 100).Value = fields
                    cmd.Parameters.Add("@RowsRevealed", SqlDbType.Int).Value = 1
                End Sub)
    End Sub

    Public Sub LogRevealBatch(userId As Integer, reason As String, fields As String, employeeIds As IReadOnlyCollection(Of Integer)) Implements IEmployeeRepository.LogRevealBatch
        Dim idsJson As String = JsonSerializer.Serialize(employeeIds)
        Execute("aud.usp_Reveal_InsertBatch",
                Sub(cmd)
                    cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = userId
                    cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, 50).Value = reason
                    cmd.Parameters.Add("@Fields", SqlDbType.NVarChar, 100).Value = fields
                    cmd.Parameters.Add("@EmployeeIdsJson", SqlDbType.NVarChar, -1).Value = idsJson
                End Sub)
    End Sub

    Private Sub Execute(procedure As String, bind As Action(Of SqlCommand))
        EmployeeSql.Execute(_factory, procedure, bind)
    End Sub

    Private Shared Sub BindFields(cmd As SqlCommand, draft As EmployeeDraft, p As EmployeePlacement, userId As Integer?)
        cmd.Parameters.Add("@FullName", SqlDbType.NVarChar, 100).Value = draft.FullName.Trim()
        cmd.Parameters.Add("@FatherOrSpouseName", SqlDbType.NVarChar, 120).Value = OptionalText(p.FatherOrSpouseName)
        cmd.Parameters.Add("@Gender", SqlDbType.Char, 1).Value = p.Gender.Trim().ToUpperInvariant()
        cmd.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value = draft.DateOfBirth.Date
        cmd.Parameters.Add("@JoinDate", SqlDbType.Date).Value = draft.JoinDate.Date
        cmd.Parameters.Add("@EmploymentTypeID", SqlDbType.TinyInt).Value = CByte(p.EmploymentTypeId)
        cmd.Parameters.Add("@WorkerTypeID", SqlDbType.TinyInt).Value = CByte(p.WorkerTypeId)
        cmd.Parameters.Add("@PayTypeID", SqlDbType.TinyInt).Value = CByte(CInt(draft.PayType))
        BindPlacementKeys(cmd, p)
        cmd.Parameters.Add("@MaritalStatusID", SqlDbType.TinyInt).Value = ByteOrNull(p.MaritalStatusId)
        cmd.Parameters.Add("@Nationality", SqlDbType.NVarChar, 30).Value = If(String.IsNullOrWhiteSpace(p.Nationality), DefaultNationality, p.Nationality.Trim())
        cmd.Parameters.Add("@Mobile", SqlDbType.Char, 10).Value = If(String.IsNullOrWhiteSpace(draft.Mobile), CObj(DBNull.Value), IdNormalizer.Compact(draft.Mobile))
        cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 200).Value = OptionalText(p.Email)
        cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = StoredProcedure.DbValue(userId)
    End Sub

    Private Shared Sub BindPlacementKeys(cmd As SqlCommand, p As EmployeePlacement)
        cmd.Parameters.Add("@FactoryID", SqlDbType.Int).Value = p.FactoryId
        cmd.Parameters.Add("@DepartmentID", SqlDbType.Int).Value = StoredProcedure.DbValue(p.DepartmentId)
        cmd.Parameters.Add("@SectionID", SqlDbType.Int).Value = StoredProcedure.DbValue(p.SectionId)
        cmd.Parameters.Add("@DesignationID", SqlDbType.Int).Value = StoredProcedure.DbValue(p.DesignationId)
        cmd.Parameters.Add("@GradeID", SqlDbType.Int).Value = StoredProcedure.DbValue(p.GradeId)
        cmd.Parameters.Add("@ReportingToEmployeeID", SqlDbType.Int).Value = StoredProcedure.DbValue(p.ReportingToEmployeeId)
        cmd.Parameters.Add("@BiometricID", SqlDbType.Int).Value = StoredProcedure.DbValue(p.BiometricId)
    End Sub

    Private Shared Sub BindId(cmd As SqlCommand, name As String, change As IdChange)
        Dim value As ProtectedId = change.Value
        cmd.Parameters.Add("@Set" & name, SqlDbType.Bit).Value = change.Kind <> IdChangeKind.Unchanged
        cmd.Parameters.Add("@" & name & "Enc", SqlDbType.VarBinary, 256).Value = If(value Is Nothing, CObj(DBNull.Value), CObj(value.Cipher))
        cmd.Parameters.Add("@" & name & "Hash", SqlDbType.Binary, 32).Value = If(value Is Nothing, CObj(DBNull.Value), CObj(value.Hash))
        cmd.Parameters.Add("@" & name & "Last4", SqlDbType.Char, 4).Value = If(value Is Nothing, CObj(DBNull.Value), value.Last4)
    End Sub

    Private Shared Function OptionalText(text As String) As Object
        Return If(String.IsNullOrWhiteSpace(text), CObj(DBNull.Value), text.Trim())
    End Function

    Private Shared Function ByteOrNull(value As Integer?) As Object
        Return If(value.HasValue, CObj(CByte(value.Value)), DBNull.Value)
    End Function

    Private Shared Function ReadSummary(reader As SqlDataReader) As EmployeeSummary
        Return New EmployeeSummary With {
            .EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeID")),
            .EmpCode = reader.GetString(reader.GetOrdinal("EmpCode")),
            .FullName = reader.GetString(reader.GetOrdinal("FullName")),
            .FactoryId = reader.GetInt32(reader.GetOrdinal("FactoryID")),
            .FactoryName = reader.GetString(reader.GetOrdinal("FactoryName")),
            .DepartmentName = EmployeeSql.OptText(reader, "DepartmentName"),
            .DesignationName = EmployeeSql.OptText(reader, "DesignationName"),
            .JoinDate = reader.GetDateTime(reader.GetOrdinal("JoinDate")),
            .Status = CType(CInt(reader.GetByte(reader.GetOrdinal("StatusID"))), EmployeeStatus),
            .IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))}
    End Function

    Private Shared Function ReadDetail(reader As SqlDataReader) As EmployeeDetail
        Dim d As New EmployeeDetail With {
            .EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeID")),
            .EmpCode = reader.GetString(reader.GetOrdinal("EmpCode")),
            .FullName = reader.GetString(reader.GetOrdinal("FullName")),
            .FatherOrSpouseName = EmployeeSql.OptText(reader, "FatherOrSpouseName"),
            .Gender = reader.GetString(reader.GetOrdinal("Gender")),
            .DateOfBirth = reader.GetDateTime(reader.GetOrdinal("DateOfBirth")),
            .JoinDate = reader.GetDateTime(reader.GetOrdinal("JoinDate")),
            .ExitDate = EmployeeSql.OptDate(reader, "ExitDate"),
            .Nationality = reader.GetString(reader.GetOrdinal("Nationality")),
            .Mobile = EmployeeSql.OptText(reader, "Mobile"),
            .Email = EmployeeSql.OptText(reader, "Email"),
            .IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
            .IsAdolescent = reader.GetBoolean(reader.GetOrdinal("IsAdolescent")),
            .RowVersion = reader.GetFieldValue(Of Byte())(reader.GetOrdinal("RowVer"))}
        ReadClasses(reader, d)
        ReadPlacement(reader, d)
        Return d
    End Function

    Private Shared Sub ReadClasses(reader As SqlDataReader, d As EmployeeDetail)
        d.EmploymentTypeId = reader.GetByte(reader.GetOrdinal("EmploymentTypeID"))
        d.WorkerTypeId = reader.GetByte(reader.GetOrdinal("WorkerTypeID"))
        d.PayType = CType(CInt(reader.GetByte(reader.GetOrdinal("PayTypeID"))), PayType)
        d.Status = CType(CInt(reader.GetByte(reader.GetOrdinal("StatusID"))), EmployeeStatus)
        d.MaritalStatusId = EmployeeSql.OptByte(reader, "MaritalStatusID")
    End Sub

    Private Shared Sub ReadPlacement(reader As SqlDataReader, d As EmployeeDetail)
        d.FactoryId = reader.GetInt32(reader.GetOrdinal("FactoryID"))
        d.FactoryName = reader.GetString(reader.GetOrdinal("FactoryName"))
        d.DepartmentId = StoredProcedure.ReadNullableInt(reader, "DepartmentID")
        d.DepartmentName = EmployeeSql.OptText(reader, "DepartmentName")
        d.SectionId = StoredProcedure.ReadNullableInt(reader, "SectionID")
        d.DesignationId = StoredProcedure.ReadNullableInt(reader, "DesignationID")
        d.DesignationName = EmployeeSql.OptText(reader, "DesignationName")
        d.GradeId = StoredProcedure.ReadNullableInt(reader, "GradeID")
        d.ReportingToEmployeeId = StoredProcedure.ReadNullableInt(reader, "ReportingToEmployeeID")
        d.BiometricId = StoredProcedure.ReadNullableInt(reader, "BiometricID")
    End Sub

End Class
