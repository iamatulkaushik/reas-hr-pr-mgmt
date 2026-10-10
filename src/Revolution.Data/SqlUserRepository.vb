Imports System.Data
Imports Microsoft.Data.SqlClient

Public NotInheritable Class SqlUserRepository
    Implements IUserRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        _factory = factory
    End Sub

    Public Function FindByUserName(userName As String) As UserRecord Implements IUserRepository.FindByUserName
        Dim result As UserRecord = Nothing
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "sec.usp_User_GetByUserName")
                cmd.Parameters.Add("@UserName", SqlDbType.NVarChar, 100).Value = userName
                Using reader = cmd.ExecuteReader()
                    If reader.Read() Then
                        result = Map(reader)
                    End If
                End Using
            End Using
        End Using
        Return result
    End Function

    Public Sub RecordFailedLogin(userId As Integer, maxAttempts As Integer, lockMinutes As Integer) Implements IUserRepository.RecordFailedLogin
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "sec.usp_User_RecordFailedLogin")
                cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = userId
                cmd.Parameters.Add("@MaxAttempts", SqlDbType.Int).Value = maxAttempts
                cmd.Parameters.Add("@LockMinutes", SqlDbType.Int).Value = lockMinutes
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Public Sub RecordSuccessfulLogin(userId As Integer) Implements IUserRepository.RecordSuccessfulLogin
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "sec.usp_User_RecordSuccessfulLogin")
                cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = userId
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Public Sub SetPassword(userId As Integer, passwordHash As Byte(), passwordSalt As Byte(), iterations As Integer,
                           mustChangePassword As Boolean) Implements IUserRepository.SetPassword
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "sec.usp_User_SetPassword")
                cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = userId
                cmd.Parameters.Add("@PasswordHash", SqlDbType.VarBinary, 32).Value = passwordHash
                cmd.Parameters.Add("@PasswordSalt", SqlDbType.VarBinary, 16).Value = passwordSalt
                cmd.Parameters.Add("@Iterations", SqlDbType.Int).Value = iterations
                cmd.Parameters.Add("@MustChangePassword", SqlDbType.Bit).Value = mustChangePassword
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Private Shared Function Map(reader As SqlDataReader) As UserRecord
        Return New UserRecord(
            reader.GetInt32(reader.GetOrdinal("UserID")),
            reader.GetString(reader.GetOrdinal("UserName")),
            StoredProcedure.ReadNullableInt(reader, "CompanyID"),
            CType(CInt(reader.GetByte(reader.GetOrdinal("RoleID"))), UserRole),
            reader.GetBoolean(reader.GetOrdinal("IsActive")),
            reader.GetBoolean(reader.GetOrdinal("MustChangePassword")),
            DirectCast(reader.GetValue(reader.GetOrdinal("PasswordHash")), Byte()),
            DirectCast(reader.GetValue(reader.GetOrdinal("PasswordSalt")), Byte()),
            reader.GetInt32(reader.GetOrdinal("Iterations")),
            StoredProcedure.ReadNullableUtc(reader, "LockedUntilUtc"))
    End Function
End Class
