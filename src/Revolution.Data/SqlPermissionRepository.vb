Imports System.Data

Public NotInheritable Class SqlPermissionRepository
    Implements IPermissionRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        _factory = factory
    End Sub

    Public Function ListForRole(role As UserRole) As IReadOnlyList(Of PermissionGrant) Implements IPermissionRepository.ListForRole
        Dim grants As New List(Of PermissionGrant)()
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "sec.usp_Permission_ListForRole")
                cmd.Parameters.Add("@RoleID", SqlDbType.TinyInt).Value = CByte(CInt(role))
                Using reader = cmd.ExecuteReader()
                    While reader.Read()
                        grants.Add(New PermissionGrant(CType(CInt(reader.GetByte(0)), ModuleKey),
                                                       CType(CInt(reader.GetByte(1)), PermissionAction)))
                    End While
                End Using
            End Using
        End Using
        Return grants
    End Function
End Class
