''' <summary>Minimum password rules: 10 characters with upper case, lower case, a digit and a symbol.</summary>
Public NotInheritable Class PasswordPolicy
    Public Const MinLength As Integer = 10

    Private Sub New()
    End Sub

    Public Shared Function Validate(password As String) As OperationResult
        If String.IsNullOrEmpty(password) OrElse password.Length < MinLength Then
            Return OperationResult.Fail($"The password must be at least {MinLength} characters.")
        End If
        If Not password.Any(Function(c) Char.IsUpper(c)) Then
            Return OperationResult.Fail("The password needs an upper-case letter.")
        End If
        If Not password.Any(Function(c) Char.IsLower(c)) Then
            Return OperationResult.Fail("The password needs a lower-case letter.")
        End If
        If Not password.Any(Function(c) Char.IsDigit(c)) Then
            Return OperationResult.Fail("The password needs a digit.")
        End If
        If Not password.Any(Function(c) Not Char.IsLetterOrDigit(c)) Then
            Return OperationResult.Fail("The password needs a symbol.")
        End If
        Return OperationResult.Ok()
    End Function
End Class
