Imports Revolution.Core

Friend NotInheritable Class FakeUserPasswordAdminRepository
    Implements IUserPasswordAdminRepository

    Public Property Users As New List(Of ResettableUser)()
    Public Property ResetCalls As New List(Of Integer)()
    Public Property LastHash As Byte()
    Public Property LastSalt As Byte()
    Public Property LastIterations As Integer
    Public Property FailWith As String

    Public Function ListResettable() As OperationResult(Of IReadOnlyList(Of ResettableUser)) Implements IUserPasswordAdminRepository.ListResettable
        Return OperationResult(Of IReadOnlyList(Of ResettableUser)).Ok(Users)
    End Function

    Public Function ResetPassword(targetUserId As Integer, passwordHash As Byte(), passwordSalt As Byte(), iterations As Integer) As OperationResult Implements IUserPasswordAdminRepository.ResetPassword
        If FailWith IsNot Nothing Then
            Return OperationResult.Fail(FailWith)
        End If
        ResetCalls.Add(targetUserId)
        LastHash = passwordHash
        LastSalt = passwordSalt
        LastIterations = iterations
        Return OperationResult.Ok()
    End Function
End Class
