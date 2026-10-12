''' <summary>A business rule enforced by the database, such as a duplicate PAN. The service turns it into a failed result.</summary>
Public NotInheritable Class EmployeeRuleException
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
End Class
