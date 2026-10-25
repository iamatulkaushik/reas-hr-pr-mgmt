''' <summary>Whole rupees in words, Indian grouping (thousand, lakh, crore), for the net pay line of a payslip. Example: 14270 is "Rupees Fourteen Thousand Two Hundred Seventy Only".</summary>
Public NotInheritable Class RupeeWords
    Private Const MaxRupees As Decimal = 999999999999D

    Private Shared ReadOnly Ones() As String = {"", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve",
                                                 "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"}
    Private Shared ReadOnly Tens() As String = {"", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"}

    Private Sub New()
    End Sub

    ''' <summary>Throws when the amount has paise or is negative or above 99,999,99,99,999 rupees. Payslip amounts are whole rupees.</summary>
    Public Shared Function Convert(amount As Decimal) As String
        If amount < 0D OrElse amount > MaxRupees OrElse amount <> Decimal.Truncate(amount) Then
            Throw New ArgumentOutOfRangeException(NameOf(amount), "Amount must be whole rupees, not negative.")
        End If
        If amount = 0D Then Return "Rupees Zero Only"
        Dim n As Long = CLng(amount)
        Dim parts As New List(Of String)()
        Dim crore As Long = n \ 10000000L
        n = n Mod 10000000L
        Dim lakh As Long = n \ 100000L
        n = n Mod 100000L
        Dim thousand As Long = n \ 1000L
        n = n Mod 1000L
        If crore > 0 Then parts.Add(Below1000(crore) & " Crore")
        If lakh > 0 Then parts.Add(Below1000(lakh) & " Lakh")
        If thousand > 0 Then parts.Add(Below1000(thousand) & " Thousand")
        If n > 0 Then parts.Add(Below1000(n))
        Return "Rupees " & String.Join(" ", parts) & " Only"
    End Function

    Private Shared Function Below1000(n As Long) As String
        Dim words As New List(Of String)()
        Dim hundreds As Integer = CInt(n \ 100L)
        Dim rest As Integer = CInt(n Mod 100L)
        If hundreds > 0 Then words.Add(Ones(hundreds) & " Hundred")
        If rest > 0 Then
            If rest < 20 Then
                words.Add(Ones(rest))
            Else
                Dim t As String = Tens(rest \ 10)
                Dim o As String = Ones(rest Mod 10)
                words.Add(If(o.Length > 0, t & " " & o, t))
            End If
        End If
        Return String.Join(" ", words)
    End Function
End Class
