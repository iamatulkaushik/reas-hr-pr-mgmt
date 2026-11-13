''' <summary>A business rule enforced by the database in an expense procedure, such as a bill already claimed. The service turns it into a failed result. ErrorNumber is the database error number (0 when unknown).</summary>
Public NotInheritable Class ExpensesRuleException
    Inherits Exception

    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    Public Sub New(message As String, errorNumber As Integer, innerException As Exception)
        MyBase.New(message, innerException)
        Me.ErrorNumber = errorNumber
    End Sub

    Public ReadOnly Property ErrorNumber As Integer
End Class
