''' <summary>The report data could not be read. The message is fit to show to the user.</summary>
Public NotInheritable Class ReportDataException
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
End Class
