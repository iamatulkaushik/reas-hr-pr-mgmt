''' <summary>Format test for an Indian mobile number: ten digits starting 6 to 9. No country code.</summary>
Public NotInheritable Class MobileValidator
    Private Const MobileLength As Integer = 10

    Private Sub New()
    End Sub

    Public Shared Function IsValid(text As String) As Boolean
        Dim number As String = IdNormalizer.Compact(text)
        If number.Length <> MobileLength OrElse Not IdNormalizer.IsAllDigits(number) Then
            Return False
        End If
        Return number(0) >= "6"c
    End Function
End Class
