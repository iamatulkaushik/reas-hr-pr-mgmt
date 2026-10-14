Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Calls the Associate-only procedures. The database raises 50030 to 50034 for an Associate check that fails; 2627 or 2601 means the user name exists.</summary>
Public NotInheritable Class SqlAssociateAdminRepository
    Implements IAssociateAdminRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        _factory = factory
    End Sub

    Public Function CreateCompany(companyName As String) As OperationResult(Of Integer) Implements IAssociateAdminRepository.CreateCompany
        Try
            Using conn = _factory.Open()
                Using cmd = StoredProcedure.CreateCommand(conn, "sec.usp_Associate_CreateCompany")
                    cmd.Parameters.Add("@CompanyName", SqlDbType.NVarChar, 200).Value = companyName
                    Return OperationResult(Of Integer).Ok(Convert.ToInt32(cmd.ExecuteScalar()))
                End Using
            End Using
        Catch ex As SqlException When IsRuleError(ex)
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
    End Function

    Public Function CreateOwner(companyId As Integer, userName As String, passwordHash As Byte(), passwordSalt As Byte(),
                                iterations As Integer) As OperationResult(Of Integer) Implements IAssociateAdminRepository.CreateOwner
        Try
            Using conn = _factory.Open()
                Using cmd = StoredProcedure.CreateCommand(conn, "sec.usp_Associate_CreateOwner")
                    cmd.Parameters.Add("@CompanyID", SqlDbType.Int).Value = companyId
                    cmd.Parameters.Add("@UserName", SqlDbType.NVarChar, 100).Value = userName
                    cmd.Parameters.Add("@PasswordHash", SqlDbType.VarBinary, 32).Value = passwordHash
                    cmd.Parameters.Add("@PasswordSalt", SqlDbType.VarBinary, 16).Value = passwordSalt
                    cmd.Parameters.Add("@Iterations", SqlDbType.Int).Value = iterations
                    Return OperationResult(Of Integer).Ok(Convert.ToInt32(cmd.ExecuteScalar()))
                End Using
            End Using
        Catch ex As SqlException When ex.Number = 2627 OrElse ex.Number = 2601
            Return OperationResult(Of Integer).Fail("That user name is already taken.")
        Catch ex As SqlException When IsRuleError(ex)
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
    End Function

    Private Shared Function IsRuleError(ex As SqlException) As Boolean
        Return ex.Number >= 50030 AndAlso ex.Number <= 50034
    End Function
End Class
