Imports System.Data
Imports Microsoft.Data.SqlClient

Public NotInheritable Class SqlLicenceRepository
    Implements ILicenceRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        _factory = factory
    End Sub

    Public Function GetInstallation() As InstallationInfo Implements ILicenceRepository.GetInstallation
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "cfg.usp_Installation_Get")
                Using reader = cmd.ExecuteReader()
                    If Not reader.Read() Then
                        Throw New InvalidOperationException("The installation id is missing. Run the database migrations.")
                    End If
                    Return New InstallationInfo(reader.GetGuid(0), DateOnly.FromDateTime(reader.GetDateTime(1)))
                End Using
            End Using
        End Using
    End Function

    Public Function GetServerToday() As DateOnly Implements ILicenceRepository.GetServerToday
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "cfg.usp_Server_Today")
                Return DateOnly.FromDateTime(CType(cmd.ExecuteScalar(), DateTime))
            End Using
        End Using
    End Function

    Public Function ListCurrent() As OperationResult(Of IReadOnlyList(Of LicenceRecord)) Implements ILicenceRepository.ListCurrent
        Dim records As New List(Of LicenceRecord)()
        Try
            Using conn = _factory.Open()
                Using cmd = StoredProcedure.CreateCommand(conn, "cfg.usp_Licence_ListCurrent")
                    Using reader = cmd.ExecuteReader()
                        Do While reader.Read()
                            records.Add(New LicenceRecord(reader.GetString(1), reader.GetString(2),
                                                          DateOnly.FromDateTime(reader.GetDateTime(3)), DateOnly.FromDateTime(reader.GetDateTime(4)),
                                                          CType(CInt(reader.GetByte(5)), PlanTier), CInt(reader.GetInt16(6)), reader.GetInt32(7),
                                                          ReadRevokedOn(reader, 9), ReadReason(reader, 10)))
                        Loop
                    End Using
                End Using
            End Using
        Catch ex As SqlException When ex.Number = 50036
            Return OperationResult(Of IReadOnlyList(Of LicenceRecord)).Fail(ex.Message)
        End Try
        Return OperationResult(Of IReadOnlyList(Of LicenceRecord)).Ok(records)
    End Function

    Public Function Import(grant As LicenceGrant, licenceText As String) As OperationResult Implements ILicenceRepository.Import
        Try
            Using conn = _factory.Open()
                Using cmd = StoredProcedure.CreateCommand(conn, "cfg.usp_Licence_Import")
                    cmd.Parameters.Add("@LicenceGuid", SqlDbType.UniqueIdentifier).Value = grant.LicenceId
                    cmd.Parameters.Add("@AssociateUserName", SqlDbType.NVarChar, 100).Value = grant.Associate
                    cmd.Parameters.Add("@LicenceText", SqlDbType.NVarChar, -1).Value = licenceText
                    cmd.Parameters.Add("@ValidFrom", SqlDbType.Date).Value = grant.ValidFrom.ToDateTime(TimeOnly.MinValue)
                    cmd.Parameters.Add("@ValidTo", SqlDbType.Date).Value = grant.ValidTo.ToDateTime(TimeOnly.MinValue)
                    cmd.Parameters.Add("@PlanCapTierID", SqlDbType.TinyInt).Value = CByte(grant.PlanCap)
                    cmd.Parameters.Add("@MaxCompanies", SqlDbType.SmallInt).Value = CShort(grant.MaxCompanies)
                    cmd.Parameters.Add("@MaxActiveUsers", SqlDbType.Int).Value = grant.MaxActiveUsers
                    cmd.ExecuteNonQuery()
                    Return OperationResult.Ok()
                End Using
            End Using
        Catch ex As SqlException When ex.Number >= 50036 AndAlso ex.Number <= 50038
            Return OperationResult.Fail(ex.Message)
        End Try
    End Function

    Public Function Revoke(associateUserName As String, reason As String) As OperationResult Implements ILicenceRepository.Revoke
        Try
            Using conn = _factory.Open()
                Using cmd = StoredProcedure.CreateCommand(conn, "cfg.usp_Licence_Revoke")
                    cmd.Parameters.Add("@AssociateUserName", SqlDbType.NVarChar, 100).Value = associateUserName
                    cmd.Parameters.Add("@Reason", SqlDbType.NVarChar, 200).Value = reason
                    cmd.ExecuteNonQuery()
                    Return OperationResult.Ok()
                End Using
            End Using
        Catch ex As SqlException When IsRevokeError(ex.Number)
            Return OperationResult.Fail(ex.Message)
        End Try
    End Function

    Public Function Reinstate(associateUserName As String) As OperationResult Implements ILicenceRepository.Reinstate
        Try
            Using conn = _factory.Open()
                Using cmd = StoredProcedure.CreateCommand(conn, "cfg.usp_Licence_Reinstate")
                    cmd.Parameters.Add("@AssociateUserName", SqlDbType.NVarChar, 100).Value = associateUserName
                    cmd.ExecuteNonQuery()
                    Return OperationResult.Ok()
                End Using
            End Using
        Catch ex As SqlException When IsRevokeError(ex.Number)
            Return OperationResult.Fail(ex.Message)
        End Try
    End Function

    ''' <summary>50036 Developer only, 50037 no such Associate, 50048 to 50051 the revoke rules (migration 0025).</summary>
    Private Shared Function IsRevokeError(number As Integer) As Boolean
        Return number = 50036 OrElse number = 50037 OrElse (number >= 50048 AndAlso number <= 50051)
    End Function

    Private Shared Function ReadRevokedOn(reader As SqlDataReader, index As Integer) As DateOnly?
        If reader.IsDBNull(index) Then
            Return Nothing
        End If
        Return DateOnly.FromDateTime(reader.GetDateTime(index))
    End Function

    Private Shared Function ReadReason(reader As SqlDataReader, index As Integer) As String
        Return If(reader.IsDBNull(index), String.Empty, reader.GetString(index))
    End Function

    Public Function FindSessionLicence() As StoredLicence Implements ILicenceRepository.FindSessionLicence
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "cfg.usp_Licence_ForSession")
                Using reader = cmd.ExecuteReader()
                    If Not reader.Read() Then
                        Return Nothing
                    End If
                    Return New StoredLicence(reader.GetString(1), reader.GetString(2), ReadRevokedOn(reader, 8), ReadReason(reader, 9))
                End Using
            End Using
        End Using
    End Function
End Class
