Imports System.Text.RegularExpressions

''' <summary>Format test for a PAN: five letters, four digits, one letter. The 4th letter is the holder type.</summary>
Public NotInheritable Class PanValidator
    Private Shared ReadOnly _pattern As New Regex("^[A-Z]{3}[ABCFGHJLPTK][A-Z][0-9]{4}[A-Z]$", RegexOptions.CultureInvariant Or RegexOptions.Compiled)

    Private Sub New()
    End Sub

    Public Shared Function IsValid(text As String) As Boolean
        Dim pan As String = IdNormalizer.Compact(text).ToUpperInvariant()
        Return _pattern.IsMatch(pan)
    End Function
End Class
