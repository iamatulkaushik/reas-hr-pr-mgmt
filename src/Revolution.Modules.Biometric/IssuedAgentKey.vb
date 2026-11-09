''' <summary>A newly issued agent key. Key is the base64 text to give to RevolutionAgent protect. It exists only in this object: show it once and drop it. It is never written to a log or an audit entry.</summary>
Public NotInheritable Class IssuedAgentKey
    Public Sub New(registrationId As Integer, key As String)
        Me.RegistrationId = registrationId
        Me.Key = key
    End Sub

    Public ReadOnly Property RegistrationId As Integer
    Public ReadOnly Property Key As String
End Class
