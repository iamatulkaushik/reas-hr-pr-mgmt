''' <summary>
''' Resetting the password of another user (ADR 0022): Developer resets Associates, an Associate resets the Owner of the open company, an Owner resets Operators.
''' Needs Edit on Security. The new password is temporary: the user must change it at the next sign-in.
''' </summary>
Public Interface IUserPasswordAdminService
    Function ListResettable() As OperationResult(Of IReadOnlyList(Of ResettableUser))
    Function ResetPassword(targetUserId As Integer, temporaryPassword As String) As OperationResult
End Interface
