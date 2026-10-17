Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Factory, department and designation storage through mst.* stored procedures (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlOrgMasterRepository
    Implements IOrgMasterRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function ListBanks() As IReadOnlyList(Of BankItem) Implements IOrgMasterRepository.ListBanks
        Return Query(Of BankItem)("mst.usp_Bank_List", AddressOf NoParameters, AddressOf ReadBank)
    End Function

    Public Function ListStates() As IReadOnlyList(Of StateItem) Implements IOrgMasterRepository.ListStates
        Return Query(Of StateItem)("mst.usp_State_List", AddressOf NoParameters, AddressOf ReadState)
    End Function

    Public Function ListFactories(activeOnly As Boolean) As IReadOnlyList(Of FactoryItem) Implements IOrgMasterRepository.ListFactories
        Return Query(Of FactoryItem)("mst.usp_Factory_List",
                     Sub(cmd) cmd.Parameters.Add("@ActiveOnly", SqlDbType.Bit).Value = activeOnly,
                     AddressOf ReadFactory)
    End Function

    Public Function SaveFactory(item As FactoryItem) As Integer Implements IOrgMasterRepository.SaveFactory
        Return SaveRow("mst.usp_Factory_Save",
                       Sub(cmd)
                           cmd.Parameters.Add("@FactoryID", SqlDbType.Int).Value = IdOrNull(item.FactoryId)
                           cmd.Parameters.Add("@FactoryCode", SqlDbType.NVarChar, 10).Value = item.Code
                           cmd.Parameters.Add("@FactoryName", SqlDbType.NVarChar, 150).Value = item.Name
                           cmd.Parameters.Add("@AddressLine1", SqlDbType.NVarChar, 150).Value = TextOrNull(item.AddressLine1)
                           cmd.Parameters.Add("@AddressLine2", SqlDbType.NVarChar, 150).Value = TextOrNull(item.AddressLine2)
                           cmd.Parameters.Add("@City", SqlDbType.NVarChar, 100).Value = TextOrNull(item.City)
                           cmd.Parameters.Add("@StateID", SqlDbType.TinyInt).Value = CByte(item.StateId)
                           cmd.Parameters.Add("@FactoryLicenceNo", SqlDbType.NVarChar, 50).Value = TextOrNull(item.LicenceNo)
                           cmd.Parameters.Add("@LicenceValidTo", SqlDbType.Date).Value = If(item.LicenceValidTo.HasValue, CObj(item.LicenceValidTo.Value), DBNull.Value)
                           cmd.Parameters.Add("@MaxWorkers", SqlDbType.Int).Value = If(item.MaxWorkers.HasValue, CObj(item.MaxWorkers.Value), DBNull.Value)
                           cmd.Parameters.Add("@ManagerName", SqlDbType.NVarChar, 120).Value = TextOrNull(item.ManagerName)
                           cmd.Parameters.Add("@EpfSubCode", SqlDbType.NVarChar, 20).Value = TextOrNull(item.EpfSubCode)
                           cmd.Parameters.Add("@EsiSubCode", SqlDbType.NVarChar, 17).Value = TextOrNull(item.EsiSubCode)
                           cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = item.IsActive
                       End Sub)
    End Function

    Public Function ListDepartments(factoryId As Integer?, activeOnly As Boolean) As IReadOnlyList(Of DepartmentItem) Implements IOrgMasterRepository.ListDepartments
        Return Query(Of DepartmentItem)("mst.usp_Department_List",
                     Sub(cmd)
                         cmd.Parameters.Add("@FactoryID", SqlDbType.Int).Value = If(factoryId.HasValue, CObj(factoryId.Value), DBNull.Value)
                         cmd.Parameters.Add("@ActiveOnly", SqlDbType.Bit).Value = activeOnly
                     End Sub,
                     AddressOf ReadDepartment)
    End Function

    Public Function SaveDepartment(item As DepartmentItem) As Integer Implements IOrgMasterRepository.SaveDepartment
        Return SaveRow("mst.usp_Department_Save",
                       Sub(cmd)
                           cmd.Parameters.Add("@DepartmentID", SqlDbType.Int).Value = IdOrNull(item.DepartmentId)
                           cmd.Parameters.Add("@FactoryID", SqlDbType.Int).Value = item.FactoryId
                           cmd.Parameters.Add("@DepartmentCode", SqlDbType.NVarChar, 10).Value = item.Code
                           cmd.Parameters.Add("@DepartmentName", SqlDbType.NVarChar, 100).Value = item.Name
                           cmd.Parameters.Add("@IsHazardous", SqlDbType.Bit).Value = item.IsHazardous
                           cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = item.IsActive
                       End Sub)
    End Function

    Public Function ListDesignations(activeOnly As Boolean) As IReadOnlyList(Of DesignationItem) Implements IOrgMasterRepository.ListDesignations
        Return Query(Of DesignationItem)("mst.usp_Designation_List",
                     Sub(cmd) cmd.Parameters.Add("@ActiveOnly", SqlDbType.Bit).Value = activeOnly,
                     AddressOf ReadDesignation)
    End Function

    Public Function SaveDesignation(item As DesignationItem) As Integer Implements IOrgMasterRepository.SaveDesignation
        Return SaveRow("mst.usp_Designation_Save",
                       Sub(cmd)
                           cmd.Parameters.Add("@DesignationID", SqlDbType.Int).Value = IdOrNull(item.DesignationId)
                           cmd.Parameters.Add("@DesignationCode", SqlDbType.NVarChar, 10).Value = item.Code
                           cmd.Parameters.Add("@DesignationName", SqlDbType.NVarChar, 100).Value = item.Name
                           cmd.Parameters.Add("@GradeID", SqlDbType.Int).Value = If(item.GradeId.HasValue, CObj(item.GradeId.Value), DBNull.Value)
                           cmd.Parameters.Add("@CategoryID", SqlDbType.TinyInt).Value = CByte(item.CategoryId)
                           cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value = item.IsActive
                       End Sub)
    End Function

    Private Function Query(Of T)(procedure As String, bind As Action(Of SqlCommand), read As Func(Of SqlDataReader, T)) As IReadOnlyList(Of T)
        Dim rows As New List(Of T)()
        EmployeeSql.Run(Sub()
                            Using conn = _factory.Open()
                                Using cmd = StoredProcedure.CreateCommand(conn, procedure)
                                    bind(cmd)
                                    Using reader = cmd.ExecuteReader()
                                        While reader.Read()
                                            rows.Add(read(reader))
                                        End While
                                    End Using
                                End Using
                            End Using
                        End Sub)
        Return rows
    End Function

    Private Function SaveRow(procedure As String, bind As Action(Of SqlCommand)) As Integer
        Dim id As Integer
        Dim got As Boolean
        EmployeeSql.Run(Sub()
                            Using conn = _factory.Open()
                                Using cmd = StoredProcedure.CreateCommand(conn, procedure)
                                    bind(cmd)
                                    Using reader = cmd.ExecuteReader()
                                        If reader.Read() Then
                                            id = reader.GetInt32(0)
                                            got = True
                                        End If
                                    End Using
                                End Using
                            End Using
                        End Sub)
        If Not got Then Throw New InvalidOperationException($"{procedure} returned no row.")
        Return id
    End Function

    Private Shared Sub NoParameters(cmd As SqlCommand)
    End Sub

    Private Shared Function ReadBank(reader As SqlDataReader) As BankItem
        Return New BankItem With {
            .BankId = reader.GetInt32(reader.GetOrdinal("BankID")),
            .Name = reader.GetString(reader.GetOrdinal("BankName"))}
    End Function

    Private Shared Function ReadState(reader As SqlDataReader) As StateItem
        Return New StateItem With {
            .StateId = CInt(reader.GetByte(reader.GetOrdinal("StateID"))),
            .Name = reader.GetString(reader.GetOrdinal("StateName"))}
    End Function

    Private Shared Function ReadFactory(reader As SqlDataReader) As FactoryItem
        Dim maxWorkers As Integer? = Nothing
        Dim at = reader.GetOrdinal("MaxWorkers")
        If Not reader.IsDBNull(at) Then maxWorkers = reader.GetInt32(at)
        Return New FactoryItem With {
            .FactoryId = reader.GetInt32(reader.GetOrdinal("FactoryID")),
            .Code = reader.GetString(reader.GetOrdinal("FactoryCode")),
            .Name = reader.GetString(reader.GetOrdinal("FactoryName")),
            .AddressLine1 = EmployeeSql.OptText(reader, "AddressLine1"),
            .AddressLine2 = EmployeeSql.OptText(reader, "AddressLine2"),
            .City = EmployeeSql.OptText(reader, "City"),
            .StateId = CInt(reader.GetByte(reader.GetOrdinal("StateID"))),
            .LicenceNo = EmployeeSql.OptText(reader, "FactoryLicenceNo"),
            .LicenceValidTo = EmployeeSql.OptDate(reader, "LicenceValidTo"),
            .MaxWorkers = maxWorkers,
            .ManagerName = EmployeeSql.OptText(reader, "ManagerName"),
            .EpfSubCode = EmployeeSql.OptText(reader, "EpfSubCode"),
            .EsiSubCode = EmployeeSql.OptText(reader, "EsiSubCode"),
            .IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))}
    End Function

    Private Shared Function ReadDepartment(reader As SqlDataReader) As DepartmentItem
        Return New DepartmentItem With {
            .DepartmentId = reader.GetInt32(reader.GetOrdinal("DepartmentID")),
            .FactoryId = reader.GetInt32(reader.GetOrdinal("FactoryID")),
            .Code = reader.GetString(reader.GetOrdinal("DepartmentCode")),
            .Name = reader.GetString(reader.GetOrdinal("DepartmentName")),
            .IsHazardous = reader.GetBoolean(reader.GetOrdinal("IsHazardous")),
            .IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))}
    End Function

    Private Shared Function ReadDesignation(reader As SqlDataReader) As DesignationItem
        Dim gradeId As Integer? = Nothing
        Dim at = reader.GetOrdinal("GradeID")
        If Not reader.IsDBNull(at) Then gradeId = reader.GetInt32(at)
        Return New DesignationItem With {
            .DesignationId = reader.GetInt32(reader.GetOrdinal("DesignationID")),
            .Code = reader.GetString(reader.GetOrdinal("DesignationCode")),
            .Name = reader.GetString(reader.GetOrdinal("DesignationName")),
            .GradeId = gradeId,
            .CategoryId = CInt(reader.GetByte(reader.GetOrdinal("CategoryID"))),
            .IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))}
    End Function

    Private Shared Function IdOrNull(id As Integer) As Object
        Return If(id = 0, CObj(DBNull.Value), id)
    End Function

    Private Shared Function TextOrNull(value As String) As Object
        Return If(String.IsNullOrWhiteSpace(value), CObj(DBNull.Value), value.Trim())
    End Function
End Class
