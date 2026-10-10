''' <summary>Holds the current user session for the running app. Nothing reads a session from anywhere else.</summary>
Public Interface ISessionContext
    ''' <summary>The signed-in user, or Nothing before login.</summary>
    ReadOnly Property Current As UserSession
    ReadOnly Property IsAuthenticated As Boolean
    Sub SignIn(session As UserSession)
    Sub SignOut()
End Interface
