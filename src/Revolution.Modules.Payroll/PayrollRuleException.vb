''' <summary>A business rule enforced by the database in a pay procedure, such as a salary revision on a date that already has one. The service turns it into a failed result. ErrorNumber is the database error number (0 when unknown) for the few cases a service must act on.</summary>
Public NotInheritable Class PayrollRuleException
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub

    Public Sub New(message As String, errorNumber As Integer, innerException As Exception)
        MyBase.New(message, innerException)
        Me.ErrorNumber = errorNumber
    End Sub

    Public ReadOnly Property ErrorNumber As Integer
End Class
