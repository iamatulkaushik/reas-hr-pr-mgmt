Imports Revolution.Core
Imports Xunit

Public Class ImportValueTests
    <Theory>
    <InlineData("2026-11-09")>
    <InlineData("09/11/2026")>
    <InlineData("9/11/2026")>
    <InlineData("09-11-2026")>
    <InlineData("09.11.2026")>
    <InlineData("09-Nov-2026")>
    <InlineData(" 9 Nov 2026 ")>
    <InlineData("2026-11-09 00:00")>
    Public Sub TryDate_ReadsTheCommonWrittenForms(text As String)
        Dim value As Date
        Assert.True(ImportValue.TryDate(text, value))
        Assert.Equal(New Date(2026, 11, 9), value)
    End Sub

    <Theory>
    <InlineData("")>
    <InlineData("   ")>
    <InlineData("31/02/2026")>
    <InlineData("13/13/2026")>
    <InlineData("01/01/1850")>
    <InlineData("not a date")>
    <InlineData("11/09/26")>
    Public Sub TryDate_RefusesWhatIsNotADate(text As String)
        Dim value As Date
        Assert.False(ImportValue.TryDate(text, value))
        Assert.Equal(Date.MinValue, value)
    End Sub

    <Fact>
    Public Sub TryDecimal_DropsIndianDigitGrouping()
        Dim value As Decimal
        Assert.True(ImportValue.TryDecimal("1,00,000.50", value))
        Assert.Equal(100000.5D, value)
        Assert.True(ImportValue.TryDecimal("-12.5", value))
        Assert.Equal(-12.5D, value)
    End Sub

    <Theory>
    <InlineData("")>
    <InlineData("abc")>
    <InlineData("12.5.5")>
    <InlineData("1e5")>
    Public Sub TryDecimal_RefusesWhatIsNotANumber(text As String)
        Dim value As Decimal
        Assert.False(ImportValue.TryDecimal(text, value))
    End Sub

    <Fact>
    Public Sub TryWholeNumber_AcceptsGroupingButNotADecimalPart()
        Dim value As Integer
        Assert.True(ImportValue.TryWholeNumber("1,200", value))
        Assert.Equal(1200, value)
        Assert.False(ImportValue.TryWholeNumber("12.5", value))
        Assert.False(ImportValue.TryWholeNumber("", value))
    End Sub

    <Theory>
    <InlineData("Yes", True)>
    <InlineData("y", True)>
    <InlineData("TRUE", True)>
    <InlineData("1", True)>
    <InlineData("No", False)>
    <InlineData("n", False)>
    <InlineData("false", False)>
    <InlineData("0", False)>
    Public Sub TryYesNo_ReadsYesAndNo(text As String, expected As Boolean)
        Dim value As Boolean
        Assert.True(ImportValue.TryYesNo(text, value))
        Assert.Equal(expected, value)
    End Sub

    <Theory>
    <InlineData("")>
    <InlineData("maybe")>
    <InlineData("2")>
    Public Sub TryYesNo_RefusesAnythingElse(text As String)
        Dim value As Boolean
        Assert.False(ImportValue.TryYesNo(text, value))
    End Sub

    <Fact>
    Public Sub Compact_RemovesSpacesAndHyphens_AndUpperCases()
        Assert.Equal("123456789012", ImportValue.Compact(" 1234 5678-9012 "))
        Assert.Equal("ABCDE1234F", ImportValue.Compact("abcde 1234-f"))
        Assert.Equal(String.Empty, ImportValue.Compact(Nothing))
    End Sub
End Class
