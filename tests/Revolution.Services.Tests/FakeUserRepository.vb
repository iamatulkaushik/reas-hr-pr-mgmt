Imports Revolution.Core

Public NotInheritable Class FakeUserRepository
    Implements IUserRepository

    Private ReadOnly _users As New Dictionary(Of String, UserRecord)(StringComparer.OrdinalIgnoreCase)

    Public ReadOnly Property FailedLogins As New List(Of Integer)()
    Public ReadOnly Property SuccessfulLogins As New List(Of Integer)()

    Public Sub Add(user As UserRecord)
        _users(user.UserName) = user
    End Sub

    Public Function FindByUserName(userName As String) As UserRecord Implements IUserRepository.FindByUserName
        Dim found As UserRecord = Nothing
        _users.TryGetValue(userName, found)
        Return found
    End Function

    Public Sub RecordFailedLogin(userId As Integer, maxAttempts As Integer, lockMinutes As Integer) Implements IUserRepository.RecordFailedLogin
        FailedLogins.Add(userId)
    End Sub

    Public Sub RecordSuccessfulLogin(userId As Integer) Implements IUserRepository.RecordSuccessfulLogin
        SuccessfulLogins.Add(userId)
    End Sub

    Public Sub SetPassword(userId As Integer, passwordHash As Byte(), passwordSalt As Byte(), iterations As Integer,
                           mustChangePassword As Boolean) Implements IUserRepository.SetPassword
        For Each existing In _users.Values.ToList()
            If existing.UserId = userId Then
                _users(existing.UserName) = New UserRecord(existing.UserId, existing.UserName, existing.CompanyId, existing.Role,
                                                           existing.IsActive, mustChangePassword, passwordHash, passwordSalt,
                                                           iterations, existing.LockedUntilUtc)
            End If
        Next
    End Sub
End Class
