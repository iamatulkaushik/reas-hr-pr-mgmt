''' <summary>A device could not be read: it did not answer, refused the connection or sent something the brand's reader could not understand. The pull goes on with the next device.</summary>
Public NotInheritable Class PunchSourceException
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
End Class
