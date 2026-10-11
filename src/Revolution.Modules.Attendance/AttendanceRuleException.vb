''' <summary>A business rule enforced by the database, such as a write into a closed month. The service turns it into a failed result.</summary>
Public NotInheritable Class AttendanceRuleException
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
End Class
