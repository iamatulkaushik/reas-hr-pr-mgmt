''' <summary>A rule of the database for the Monthly Pack was broken (errors 53650 to 53699). The message is written for the user.</summary>
Public NotInheritable Class PackRuleException
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
End Class
