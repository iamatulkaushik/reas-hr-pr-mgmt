''' <summary>Format test for an ESIC insurance (IP) number: ten digits.</summary>
Public NotInheritable Class EsiIpValidator
    Private Const IpLength As Integer = 10

    Private Sub New()
    End Sub

    Public Shared Function IsValid(text As String) As Boolean
        Dim number As String = IdNormalizer.Compact(text)
        Return number.Length = IpLength AndAlso IdNormalizer.IsAllDigits(number)
    End Function
End Class
