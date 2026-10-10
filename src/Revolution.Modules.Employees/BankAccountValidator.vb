''' <summary>Format test for a bank account number: nine to eighteen digits.</summary>
Public NotInheritable Class BankAccountValidator
    Private Const MinimumLength As Integer = 9
    Private Const MaximumLength As Integer = 18

    Private Sub New()
    End Sub

    Public Shared Function IsValid(text As String) As Boolean
        Dim number As String = IdNormalizer.Compact(text)
        Return number.Length >= MinimumLength AndAlso number.Length <= MaximumLength AndAlso IdNormalizer.IsAllDigits(number)
    End Function
End Class
