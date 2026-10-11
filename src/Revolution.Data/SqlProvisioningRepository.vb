Imports System.Data

Public NotInheritable Class SqlProvisioningRepository
    Implements IProvisioningRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        _factory = factory
    End Sub

    Public Function CreateCompany(companyName As String, plan As PlanTier) As Integer Implements IProvisioningRepository.CreateCompany
        Dim id As Integer
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "mst.usp_Company_Create")
                cmd.Parameters.Add("@CompanyName", SqlDbType.NVarChar, 200).Value = companyName
                cmd.Parameters.Add("@PlanTierID", SqlDbType.TinyInt).Value = CByte(CInt(plan))
                id = Convert.ToInt32(cmd.ExecuteScalar())
            End Using
        End Using
        Return id
    End Function

    Public Function CreateUser(userName As String, companyId As Integer?, role As UserRole, passwordHash As Byte(),
                               passwordSalt As Byte(), iterations As Integer,
                               mustChangePassword As Boolean) As Integer Implements IProvisioningRepository.CreateUser
        Dim id As Integer
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "sec.usp_User_Create")
                cmd.Parameters.Add("@UserName", SqlDbType.NVarChar, 100).Value = userName
                cmd.Parameters.Add("@CompanyID", SqlDbType.Int).Value = StoredProcedure.DbValue(companyId)
                cmd.Parameters.Add("@RoleID", SqlDbType.TinyInt).Value = CByte(CInt(role))
                cmd.Parameters.Add("@PasswordHash", SqlDbType.VarBinary, 32).Value = passwordHash
                cmd.Parameters.Add("@PasswordSalt", SqlDbType.VarBinary, 16).Value = passwordSalt
                cmd.Parameters.Add("@Iterations", SqlDbType.Int).Value = iterations
                cmd.Parameters.Add("@MustChangePassword", SqlDbType.Bit).Value = mustChangePassword
                id = Convert.ToInt32(cmd.ExecuteScalar())
            End Using
        End Using
        Return id
    End Function
End Class
