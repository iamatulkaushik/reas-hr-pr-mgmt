Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Calls sec.usp_User_ListResettable and sec.usp_User_ResetPassword. The database raises 50060 to 50062 when the caller may not reset that user.</summary>
Public NotInheritable Class SqlUserPasswordAdminRepository
    Implements IUserPasswordAdminRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function ListResettable() As OperationResult(Of IReadOnlyList(Of ResettableUser)) Implements IUserPasswordAdminRepository.ListResettable
        Try
            Dim items As New List(Of ResettableUser)()
            Using conn = _factory.Open()
                Using cmd = StoredProcedure.CreateCommand(conn, "sec.usp_User_ListResettable")
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            items.Add(New ResettableUser(reader.GetInt32(reader.GetOrdinal("UserID")), reader.GetString(reader.GetOrdinal("UserName")),
                                                         CType(Convert.ToInt32(reader("RoleID")), UserRole), reader.GetBoolean(reader.GetOrdinal("IsLocked")),
                                                         reader.GetBoolean(reader.GetOrdinal("MustChangePassword"))))
                        End While
                    End Using
                End Using
            End Using
            Return OperationResult(Of IReadOnlyList(Of ResettableUser)).Ok(items)
        Catch ex As SqlException When IsRuleError(ex)
            Return OperationResult(Of IReadOnlyList(Of ResettableUser)).Fail(ex.Message)
        End Try
    End Function

    Public Function ResetPassword(targetUserId As Integer, passwordHash As Byte(), passwordSalt As Byte(), iterations As Integer) As OperationResult Implements IUserPasswordAdminRepository.ResetPassword
        Try
            Using conn = _factory.Open()
                Using cmd = StoredProcedure.CreateCommand(conn, "sec.usp_User_ResetPassword")
                    cmd.Parameters.Add("@TargetUserID", SqlDbType.Int).Value = targetUserId
                    cmd.Parameters.Add("@PasswordHash", SqlDbType.VarBinary, 32).Value = passwordHash
                    cmd.Parameters.Add("@PasswordSalt", SqlDbType.VarBinary, 16).Value = passwordSalt
                    cmd.Parameters.Add("@Iterations", SqlDbType.Int).Value = iterations
                    cmd.ExecuteNonQuery()
                End Using
            End Using
            Return OperationResult.Ok()
        Catch ex As SqlException When IsRuleError(ex)
            Return OperationResult.Fail(ex.Message)
        End Try
    End Function

    Private Shared Function IsRuleError(ex As SqlException) As Boolean
        Return ex.Number >= 50060 AndAlso ex.Number <= 50062
    End Function
End Class
