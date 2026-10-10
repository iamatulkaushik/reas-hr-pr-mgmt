''' <summary>Format test for an EPFO Universal Account Number: twelve digits.</summary>
Public NotInheritable Class UanValidator
    Private Const UanLength As Integer = 12

    Private Sub New()
    End Sub

    Public Shared Function IsValid(text As String) As Boolean
        Dim number As String = IdNormalizer.Compact(text)
        Return number.Length = UanLength AndAlso IdNormalizer.IsAllDigits(number)
    End Function
End Class
