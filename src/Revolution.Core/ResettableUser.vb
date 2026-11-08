''' <summary>A user whose password the signed-in user may reset (ADR 0022). The database decides who is on the list.</summary>
Public NotInheritable Class ResettableUser
    Public Sub New(userId As Integer, userName As String, role As UserRole, isLocked As Boolean, mustChangePassword As Boolean)
        Me.UserId = userId
        Me.UserName = If(userName, String.Empty)
        Me.Role = role
        Me.IsLocked = isLocked
        Me.MustChangePassword = mustChangePassword
    End Sub

    Public ReadOnly Property UserId As Integer
    Public ReadOnly Property UserName As String
    Public ReadOnly Property Role As UserRole
    Public ReadOnly Property IsLocked As Boolean
    ''' <summary>True while a temporary password is still waiting to be changed.</summary>
    Public ReadOnly Property MustChangePassword As Boolean
End Class
