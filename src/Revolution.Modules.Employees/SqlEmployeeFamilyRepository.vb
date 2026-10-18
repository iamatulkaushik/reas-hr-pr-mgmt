Imports System.Data
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Family and nominee storage through stored procedures. Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlEmployeeFamilyRepository
    Implements IEmployeeFamilyRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function ListFamily(employeeId As Integer) As IReadOnlyList(Of FamilyMember) Implements IEmployeeFamilyRepository.ListFamily
        Dim rows As New List(Of FamilyMember)()
        EmployeeSql.Run(Sub()
                            Using conn = _factory.Open()
                                Using cmd = StoredProcedure.CreateCommand(conn, "mst.usp_EmployeeFamily_List")
                                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                                    Using reader = cmd.ExecuteReader()
                                        While reader.Read()
                                            rows.Add(ReadMember(reader))
                                        End While
                                    End Using
                                End Using
                            End Using
                        End Sub)
        Return rows
    End Function

    Public Function SaveFamily(employeeId As Integer, member As FamilyMember) As Integer Implements IEmployeeFamilyRepository.SaveFamily
        Dim id As Integer
        EmployeeSql.Run(Sub()
                            Using conn = _factory.Open()
                                Using cmd = StoredProcedure.CreateCommand(conn, "mst.usp_EmployeeFamily_Save")
                                    cmd.Parameters.Add("@FamilyID", SqlDbType.Int).Value = If(member.FamilyId = 0, CObj(DBNull.Value), member.FamilyId)
                                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                                    cmd.Parameters.Add("@MemberName", SqlDbType.NVarChar, 120).Value = member.Name
                                    cmd.Parameters.Add("@Relation", SqlDbType.NVarChar, 30).Value = member.Relation
                                    cmd.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value = If(member.DateOfBirth.HasValue, CObj(member.DateOfBirth.Value), DBNull.Value)
                                    cmd.Parameters.Add("@IsDependent", SqlDbType.Bit).Value = member.IsDependent
                                    cmd.Parameters.Add("@IsResiding", SqlDbType.Bit).Value = member.IsResiding
                                    Using reader = cmd.ExecuteReader()
                                        If reader.Read() Then id = reader.GetInt32(0)
                                    End Using
                                End Using
                            End Using
                        End Sub)
        If id = 0 Then Throw New InvalidOperationException("mst.usp_EmployeeFamily_Save returned no row.")
        Return id
    End Function

    Public Sub DeleteFamily(familyId As Integer) Implements IEmployeeFamilyRepository.DeleteFamily
        RunDelete("mst.usp_EmployeeFamily_Delete", "@FamilyID", familyId)
    End Sub

    Public Function ListNominees(employeeId As Integer) As IReadOnlyList(Of NomineeRow) Implements IEmployeeFamilyRepository.ListNominees
        Dim rows As New List(Of NomineeRow)()
        EmployeeSql.Run(Sub()
                            Using conn = _factory.Open()
                                Using cmd = StoredProcedure.CreateCommand(conn, "mst.usp_EmployeeNominee_List")
                                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                                    Using reader = cmd.ExecuteReader()
                                        While reader.Read()
                                            rows.Add(ReadNominee(reader))
                                        End While
                                    End Using
                                End Using
                            End Using
                        End Sub)
        Return rows
    End Function

    Public Sub SaveNominee(employeeId As Integer, scheme As NomineeScheme, familyId As Integer, sharePercent As Decimal) Implements IEmployeeFamilyRepository.SaveNominee
        EmployeeSql.Run(Sub()
                            Using conn = _factory.Open()
                                Using cmd = StoredProcedure.CreateCommand(conn, "mst.usp_EmployeeNominee_Save")
                                    cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = employeeId
                                    cmd.Parameters.Add("@SchemeID", SqlDbType.TinyInt).Value = CByte(scheme)
                                    cmd.Parameters.Add("@FamilyID", SqlDbType.Int).Value = familyId
                                    Dim share = cmd.Parameters.Add("@SharePercent", SqlDbType.Decimal)
                                    share.Precision = 5
                                    share.Scale = 2
                                    share.Value = sharePercent
                                    cmd.ExecuteNonQuery()
                                End Using
                            End Using
                        End Sub)
    End Sub

    Public Sub DeleteNominee(nomineeId As Integer) Implements IEmployeeFamilyRepository.DeleteNominee
        RunDelete("mst.usp_EmployeeNominee_Delete", "@NomineeID", nomineeId)
    End Sub

    Private Sub RunDelete(procedure As String, parameterName As String, id As Integer)
        EmployeeSql.Run(Sub()
                            Using conn = _factory.Open()
                                Using cmd = StoredProcedure.CreateCommand(conn, procedure)
                                    cmd.Parameters.Add(parameterName, SqlDbType.Int).Value = id
                                    cmd.ExecuteNonQuery()
                                End Using
                            End Using
                        End Sub)
    End Sub

    Private Shared Function ReadMember(reader As SqlDataReader) As FamilyMember
        Return New FamilyMember With {
            .FamilyId = reader.GetInt32(reader.GetOrdinal("FamilyID")),
            .Name = reader.GetString(reader.GetOrdinal("MemberName")),
            .Relation = reader.GetString(reader.GetOrdinal("Relation")),
            .DateOfBirth = EmployeeSql.OptDate(reader, "DateOfBirth"),
            .IsDependent = reader.GetBoolean(reader.GetOrdinal("IsDependent")),
            .IsResiding = reader.GetBoolean(reader.GetOrdinal("IsResiding"))}
    End Function

    Private Shared Function ReadNominee(reader As SqlDataReader) As NomineeRow
        Return New NomineeRow With {
            .NomineeId = reader.GetInt32(reader.GetOrdinal("NomineeID")),
            .Scheme = CType(CInt(reader.GetByte(reader.GetOrdinal("SchemeID"))), NomineeScheme),
            .FamilyId = reader.GetInt32(reader.GetOrdinal("FamilyID")),
            .MemberName = reader.GetString(reader.GetOrdinal("MemberName")),
            .Relation = reader.GetString(reader.GetOrdinal("Relation")),
            .SharePercent = reader.GetDecimal(reader.GetOrdinal("SharePercent"))}
    End Function
End Class
