Imports Revolution.Modules.Employees
Imports Xunit

Public Class IdValidatorTests
    <Theory>
    <InlineData("234567890124")>
    <InlineData("2345 6789 0124")>
    <InlineData("2345-6789-0124")>
    <InlineData("987654321096")>
    Public Sub Aadhaar_Valid(text As String)
        Assert.True(AadhaarValidator.IsValid(text))
    End Sub

    <Theory>
    <InlineData("234567890125")>
    <InlineData("234567890142")>
    <InlineData("123456789012")>
    <InlineData("23456789012")>
    <InlineData("23456789012A")>
    <InlineData("")>
    <InlineData(Nothing)>
    Public Sub Aadhaar_Invalid(text As String)
        Assert.False(AadhaarValidator.IsValid(text))
    End Sub

    <Theory>
    <InlineData("ABCPE1234F", True)>
    <InlineData("abcpe1234f", True)>
    <InlineData("ABCDE1234F", False)>
    <InlineData("ABCPE12345", False)>
    <InlineData("ABCP1234FE", False)>
    <InlineData("", False)>
    Public Sub Pan_Format(text As String, expected As Boolean)
        Assert.Equal(expected, PanValidator.IsValid(text))
    End Sub

    <Theory>
    <InlineData("SBIN0001234", True)>
    <InlineData("sbin0abc123", True)>
    <InlineData("SBIN1001234", False)>
    <InlineData("SBI00001234", False)>
    <InlineData("SBIN000123", False)>
    Public Sub Ifsc_Format(text As String, expected As Boolean)
        Assert.Equal(expected, IfscValidator.IsValid(text))
    End Sub

    <Theory>
    <InlineData("100123456789", True)>
    <InlineData("10012345678", False)>
    <InlineData("10012345678A", False)>
    Public Sub Uan_Format(text As String, expected As Boolean)
        Assert.Equal(expected, UanValidator.IsValid(text))
    End Sub

    <Theory>
    <InlineData("1234567890", True)>
    <InlineData("123456789", False)>
    <InlineData("12345678901", False)>
    Public Sub EsiIp_Format(text As String, expected As Boolean)
        Assert.Equal(expected, EsiIpValidator.IsValid(text))
    End Sub

    <Theory>
    <InlineData("9876543210", True)>
    <InlineData("6000000000", True)>
    <InlineData("5876543210", False)>
    <InlineData("987654321", False)>
    Public Sub Mobile_Format(text As String, expected As Boolean)
        Assert.Equal(expected, MobileValidator.IsValid(text))
    End Sub

    <Theory>
    <InlineData("123456789", True)>
    <InlineData("123456789012345678", True)>
    <InlineData("12345678", False)>
    <InlineData("1234567890123456789", False)>
    Public Sub BankAccount_Length(text As String, expected As Boolean)
        Assert.Equal(expected, BankAccountValidator.IsValid(text))
    End Sub

    <Theory>
    <InlineData("234567890124", "XXXX-XXXX-0124")>
    <InlineData("ABCPE1234F", "XXXX-XX-234F")>
    <InlineData("1234", "XXXX")>
    <InlineData("", "")>
    Public Sub Mask_ShowsLastFour(text As String, expected As String)
        Assert.Equal(expected, SensitiveIdMask.Mask(text))
    End Sub
End Class
