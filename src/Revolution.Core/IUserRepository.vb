''' <summary>User storage. Implemented in Revolution.Data with stored procedures.</summary>
Public Interface IUserRepository
    ''' <summary>Returns Nothing when the user name does not exist.</summary>
    Function FindByUserName(userName As String) As UserRecord
    ''' <summary>Counts a failure and locks the account for lockMinutes once maxAttempts is reached.</summary>
    Sub RecordFailedLogin(userId As Integer, maxAttempts As Integer, lockMinutes As Integer)
    Sub RecordSuccessfulLogin(userId As Integer)
    Sub SetPassword(userId As Integer, passwordHash As Byte(), passwordSalt As Byte(), iterations As Integer, mustChangePassword As Boolean)
End Interface
