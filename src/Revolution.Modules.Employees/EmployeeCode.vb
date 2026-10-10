Imports System.Globalization

''' <summary>Builds and reads employee codes such as EMP-00042. Uniqueness per company is enforced in the database.</summary>
Public NotInheritable Class EmployeeCode
    Private Const SequenceDigits As Integer = 5
    Private Const Separator As String = "-"

    Private Sub New()
    End Sub

    Public Shared Function Build(prefix As String, sequence As Integer) As String
        If String.IsNullOrWhiteSpace(prefix) Then
            Throw New ArgumentException("A code prefix is required.", NameOf(prefix))
        End If
        If sequence < 1 Then
            Throw New ArgumentOutOfRangeException(NameOf(sequence), "Sequence starts at 1.")
        End If
        Return prefix.Trim().ToUpperInvariant() & Separator & sequence.ToString("D" & SequenceDigits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)
    End Function

    Public Shared Function TryParseSequence(code As String, prefix As String, ByRef sequence As Integer) As Boolean
        sequence = 0
        If String.IsNullOrWhiteSpace(code) OrElse String.IsNullOrWhiteSpace(prefix) Then
            Return False
        End If
        Dim head As String = prefix.Trim().ToUpperInvariant() & Separator
        Dim text As String = code.Trim().ToUpperInvariant()
        If Not text.StartsWith(head, StringComparison.Ordinal) Then
            Return False
        End If
        Return Integer.TryParse(text.Substring(head.Length), NumberStyles.None, CultureInfo.InvariantCulture, sequence) AndAlso sequence >= 1
    End Function
End Class
