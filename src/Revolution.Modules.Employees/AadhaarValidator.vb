''' <summary>Format and checksum test for an Aadhaar number. It cannot prove the number was issued.</summary>
Public NotInheritable Class AadhaarValidator
    Private Const AadhaarLength As Integer = 12

    Private Sub New()
    End Sub

    Public Shared Function IsValid(text As String) As Boolean
        Dim number As String = IdNormalizer.Compact(text)
        If number.Length <> AadhaarLength OrElse Not IdNormalizer.IsAllDigits(number) Then
            Return False
        End If
        If number(0) < "2"c Then
            Return False
        End If
        Return VerhoeffChecksum.IsValid(number)
    End Function
End Class
