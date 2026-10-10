Imports System.Security.Cryptography

''' <summary>PBKDF2-SHA256 password hashing. The iteration count is stored per user so it can be raised later.</summary>
Public NotInheritable Class PasswordHasher
    Public Const CurrentIterations As Integer = 210000
    Private Const SaltSize As Integer = 16
    Private Const HashSize As Integer = 32

    Public Function Create(password As String) As PasswordHashResult
        Dim salt = RandomNumberGenerator.GetBytes(SaltSize)
        Return New PasswordHashResult(Derive(password, salt, CurrentIterations), salt, CurrentIterations)
    End Function

    Public Function Verify(password As String, hash As Byte(), salt As Byte(), iterations As Integer) As Boolean
        If hash Is Nothing OrElse salt Is Nothing OrElse iterations <= 0 Then
            Return False
        End If
        Return CryptographicOperations.FixedTimeEquals(Derive(password, salt, iterations), hash)
    End Function

    ''' <summary>Spends the same time as a real check, so an unknown user name cannot be told apart by timing.</summary>
    Public Sub Equalize(password As String)
        Derive(password, New Byte(SaltSize - 1) {}, CurrentIterations)
    End Sub

    Private Shared Function Derive(password As String, salt As Byte(), iterations As Integer) As Byte()
        Return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashSize)
    End Function
End Class
