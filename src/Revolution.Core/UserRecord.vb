''' <summary>A user row as stored, including password material. Never shown in a UI.</summary>
Public NotInheritable Class UserRecord
    Public Sub New(userId As Integer, userName As String, companyId As Integer?, role As UserRole, isActive As Boolean,
                   mustChangePassword As Boolean, passwordHash As Byte(), passwordSalt As Byte(), iterations As Integer,
                   lockedUntilUtc As DateTime?)
        Me.UserId = userId
        Me.UserName = userName
        Me.CompanyId = companyId
        Me.Role = role
        Me.IsActive = isActive
        Me.MustChangePassword = mustChangePassword
        Me.PasswordHash = passwordHash
        Me.PasswordSalt = passwordSalt
        Me.Iterations = iterations
        Me.LockedUntilUtc = lockedUntilUtc
    End Sub

    Public ReadOnly Property UserId As Integer
    Public ReadOnly Property UserName As String
    Public ReadOnly Property CompanyId As Integer?
    Public ReadOnly Property Role As UserRole
    Public ReadOnly Property IsActive As Boolean
    Public ReadOnly Property MustChangePassword As Boolean
    Public ReadOnly Property PasswordHash As Byte()
    Public ReadOnly Property PasswordSalt As Byte()
    Public ReadOnly Property Iterations As Integer
    Public ReadOnly Property LockedUntilUtc As DateTime?
End Class
