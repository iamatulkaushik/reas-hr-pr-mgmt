''' <summary>Holds the current user session for the running app.</summary>
Public NotInheritable Class SessionContext
    Implements ISessionContext

    Private _current As UserSession

    Public ReadOnly Property Current As UserSession Implements ISessionContext.Current
        Get
            Return _current
        End Get
    End Property

    Public ReadOnly Property IsAuthenticated As Boolean Implements ISessionContext.IsAuthenticated
        Get
            Return _current IsNot Nothing
        End Get
    End Property

    Public Sub SignIn(session As UserSession) Implements ISessionContext.SignIn
        If session Is Nothing Then
            Throw New ArgumentNullException(NameOf(session))
        End If
        _current = session
    End Sub

    Public Sub SignOut() Implements ISessionContext.SignOut
        _current = Nothing
    End Sub
End Class
