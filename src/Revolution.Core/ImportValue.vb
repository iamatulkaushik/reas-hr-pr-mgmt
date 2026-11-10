Imports System.Globalization

''' <summary>
''' Reads the text of an import cell the way people write it in India (ADR 0025): dates as dd/mm/yyyy, dd-mm-yyyy, dd-Mon-yyyy or yyyy-mm-dd, numbers with Indian digit
''' grouping, yes or no. A file reader hands every cell over as text, so every module reads values through these.
''' </summary>
Public NotInheritable Class ImportValue
    Private Shared ReadOnly DateFormats As String() = {
        "yyyy-MM-dd", "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss",
        "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "dd.MM.yyyy", "d.M.yyyy",
        "dd-MMM-yyyy", "d-MMM-yyyy", "dd MMM yyyy", "d MMM yyyy"}

    Private Sub New()
    End Sub

    ''' <summary>A calendar date from 1900 on. False for blank text, a date that does not exist (31/02/2026) and a year before 1900.</summary>
    Public Shared Function TryDate(text As String, ByRef value As Date) As Boolean
        value = Date.MinValue
        If String.IsNullOrWhiteSpace(text) Then
            Return False
        End If
        Dim parsed As Date
        If Not Date.TryParseExact(text.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, parsed) OrElse parsed.Year < 1900 Then
            Return False
        End If
        value = parsed.Date
        Return True
    End Function

    ''' <summary>A decimal number. Commas are digit grouping and are dropped, so 1,00,000.50 reads as 100000.50. A sign and one decimal point are allowed; nothing else.</summary>
    Public Shared Function TryDecimal(text As String, ByRef value As Decimal) As Boolean
        value = 0D
        If String.IsNullOrWhiteSpace(text) Then
            Return False
        End If
        Return Decimal.TryParse(text.Trim().Replace(",", String.Empty), NumberStyles.AllowLeadingSign Or NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, value)
    End Function

    ''' <summary>A whole number (no decimal part), with digit grouping allowed.</summary>
    Public Shared Function TryWholeNumber(text As String, ByRef value As Integer) As Boolean
        value = 0
        If String.IsNullOrWhiteSpace(text) Then
            Return False
        End If
        Return Integer.TryParse(text.Trim().Replace(",", String.Empty), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, value)
    End Function

    ''' <summary>Yes, Y, True or 1 is True; No, N, False or 0 is False. Anything else, and blank text, is not a yes or no.</summary>
    Public Shared Function TryYesNo(text As String, ByRef value As Boolean) As Boolean
        value = False
        If String.IsNullOrWhiteSpace(text) Then
            Return False
        End If
        Select Case text.Trim().ToLowerInvariant()
            Case "yes", "y", "true", "1"
                value = True
                Return True
            Case "no", "n", "false", "0"
                value = False
                Return True
            Case Else
                Return False
        End Select
    End Function

    ''' <summary>An id as typed: spaces and hyphens removed (1234 5678 9012 and 1234-5678-9012 become 123456789012), upper case.</summary>
    Public Shared Function Compact(text As String) As String
        If String.IsNullOrWhiteSpace(text) Then
            Return String.Empty
        End If
        Dim kept As New List(Of Char)()
        For Each c As Char In text
            If Not Char.IsWhiteSpace(c) AndAlso c <> "-"c Then
                kept.Add(Char.ToUpperInvariant(c))
            End If
        Next
        Return New String(kept.ToArray())
    End Function
End Class
