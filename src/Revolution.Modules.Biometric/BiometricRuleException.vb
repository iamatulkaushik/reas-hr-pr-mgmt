''' <summary>A business rule enforced by the database in a biometric procedure, such as a card already on another employee. The service turns it into a failed result. ErrorNumber is the database error number (0 when unknown).</summary>
Public NotInheritable Class BiometricRuleException
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
