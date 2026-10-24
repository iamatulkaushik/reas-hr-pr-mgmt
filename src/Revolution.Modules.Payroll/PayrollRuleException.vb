''' <summary>A business rule enforced by the database in a pay procedure, such as a salary revision on a date that already has one. The service turns it into a failed result.</summary>
Public NotInheritable Class PayrollRuleException
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub
End Class
