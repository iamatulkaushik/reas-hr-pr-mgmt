Imports Revolution.Modules.Payroll
Imports Xunit

Public Class RupeeWordsTests
    <Theory>
    <InlineData(0, "Rupees Zero Only")>
    <InlineData(1, "Rupees One Only")>
    <InlineData(19, "Rupees Nineteen Only")>
    <InlineData(20, "Rupees Twenty Only")>
    <InlineData(21, "Rupees Twenty One Only")>
    <InlineData(100, "Rupees One Hundred Only")>
    <InlineData(101, "Rupees One Hundred One Only")>
    <InlineData(999, "Rupees Nine Hundred Ninety Nine Only")>
    <InlineData(1000, "Rupees One Thousand Only")>
    <InlineData(14270, "Rupees Fourteen Thousand Two Hundred Seventy Only")>
    <InlineData(99999, "Rupees Ninety Nine Thousand Nine Hundred Ninety Nine Only")>
    <InlineData(100000, "Rupees One Lakh Only")>
    <InlineData(123456, "Rupees One Lakh Twenty Three Thousand Four Hundred Fifty Six Only")>
    <InlineData(10000000, "Rupees One Crore Only")>
    <InlineData(10100001, "Rupees One Crore One Lakh One Only")>
    Public Sub Convert_UsesIndianGrouping(amount As Decimal, expected As String)
        Assert.Equal(expected, RupeeWords.Convert(amount))
    End Sub

    <Fact>
    Public Sub Convert_Paise_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Sub() RupeeWords.Convert(10.5D))
    End Sub

    <Fact>
    Public Sub Convert_Negative_Throws()
        Assert.Throws(Of ArgumentOutOfRangeException)(Sub() RupeeWords.Convert(-1D))
    End Sub

    <Fact>
    Public Sub IdMask_ShowsLastFour()
        Assert.Equal("XXXXXXXX6789", IdMask.Last4("6789"))
    End Sub

    <Theory>
    <InlineData("")>
    <InlineData("  ")>
    Public Sub IdMask_NoLastFour_IsEmpty(value As String)
        Assert.Equal(String.Empty, IdMask.Last4(value))
    End Sub

    <Fact>
    Public Sub IdMask_Null_IsEmpty()
        Assert.Equal(String.Empty, IdMask.Last4(Nothing))
    End Sub
End Class
