Imports System.Text.RegularExpressions

''' <summary>Format test for an IFSC: four bank letters, a zero, six branch characters.</summary>
Public NotInheritable Class IfscValidator
    Private Shared ReadOnly _pattern As New Regex("^[A-Z]{4}0[A-Z0-9]{6}$", RegexOptions.CultureInvariant Or RegexOptions.Compiled)

    Private Sub New()
    End Sub

    Public Shared Function IsValid(text As String) As Boolean
        Dim code As String = IdNormalizer.Compact(text).ToUpperInvariant()
        Return _pattern.IsMatch(code)
    End Function
End Class
