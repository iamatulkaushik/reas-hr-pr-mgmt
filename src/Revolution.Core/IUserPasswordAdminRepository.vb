''' <summary>Calls the reset procedures (migration 0031). The database checks who may reset whom; the app only passes the new hash.</summary>
Public Interface IUserPasswordAdminRepository
    Function ListResettable() As OperationResult(Of IReadOnlyList(Of ResettableUser))
    Function ResetPassword(targetUserId As Integer, passwordHash As Byte(), passwordSalt As Byte(), iterations As Integer) As OperationResult
End Interface
