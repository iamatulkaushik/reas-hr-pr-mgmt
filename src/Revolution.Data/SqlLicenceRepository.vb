Imports System.Data

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

    Public Function FindSessionLicence() As StoredLicence Implements ILicenceRepository.FindSessionLicence
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "cfg.usp_Licence_ForSession")
                Using reader = cmd.ExecuteReader()
                    If Not reader.Read() Then
                        Return Nothing
                    End If
                    Return New StoredLicence(reader.GetString(1), reader.GetString(2))
                End Using
            End Using
        End Using
    End Function
End Class
