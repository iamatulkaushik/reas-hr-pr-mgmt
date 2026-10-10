''' <summary>A freshly derived password hash with its salt and iteration count.</summary>
Public NotInheritable Class PasswordHashResult
    Public Sub New(hash As Byte(), salt As Byte(), iterations As Integer)
        Me.Hash = hash
        Me.Salt = salt
        Me.Iterations = iterations
    End Sub

    Public ReadOnly Property Hash As Byte()
    Public ReadOnly Property Salt As Byte()
    Public ReadOnly Property Iterations As Integer
End Class
