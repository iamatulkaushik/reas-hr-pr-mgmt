''' <summary>Ids in clear after a permitted, logged reveal. Never log or keep these. Empty text means not asked for or not on file.</summary>
Public NotInheritable Class RevealedIds
    Public Property Uan As String = String.Empty
    Public Property EsiIp As String = String.Empty
    Public Property Pan As String = String.Empty
    Public Property Aadhaar As String = String.Empty

    Public ReadOnly Property Any As Boolean
        Get
            Return Uan.Length > 0 OrElse EsiIp.Length > 0 OrElse Pan.Length > 0 OrElse Aadhaar.Length > 0
        End Get
    End Property
End Class
