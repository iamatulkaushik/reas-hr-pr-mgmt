Imports Revolution.Modules.Employees
Imports Xunit

Public Class EmployeeValidatorTests
    Private Shared ReadOnly Today As New Date(2026, 10, 10)

    Private Shared Function ValidDraft() As EmployeeDraft
        Return New EmployeeDraft With {
            .FullName = "Ravi Kumar",
            .DateOfBirth = New Date(1995, 4, 12),
            .JoinDate = New Date(2026, 4, 1),
            .PayType = PayType.MonthlySalary,
            .Mobile = "9876543210",
            .Pan = "ABCPE1234F",
            .Aadhaar = "234567890124",
            .Uan = "100123456789",
            .EsiIp = "1234567890",
            .BankAccount = "123456789012",
            .Ifsc = "SBIN0001234"
        }
    End Function

    <Fact>
    Public Sub FullyValidDraft_HasNoProblems()
        Assert.Empty(EmployeeValidator.Validate(ValidDraft(), Today))
    End Sub

    <Fact>
    Public Sub OptionalIds_CanBeBlank()
        Dim d = ValidDraft()
        d.Mobile = ""
        d.Pan = ""
        d.Aadhaar = ""
        d.Uan = ""
        d.EsiIp = ""
        d.BankAccount = ""
        d.Ifsc = ""
        Assert.Empty(EmployeeValidator.Validate(d, Today))
    End Sub

    <Fact>
    Public Sub BlankName_Fails()
        Dim d = ValidDraft()
        d.FullName = "  "
        Assert.Contains("Enter the employee name.", EmployeeValidator.Validate(d, Today))
    End Sub

    <Fact>
    Public Sub UnderAgeOnJoining_Fails()
        Dim d = ValidDraft()
        d.DateOfBirth = New Date(2015, 1, 1)
        d.JoinDate = New Date(2026, 4, 1)
        Assert.Contains("Employee must be at least 14 years old on joining.", EmployeeValidator.Validate(d, Today))
    End Sub

    <Fact>
    Public Sub FourteenthBirthdayJoining_Passes()
        Dim d = ValidDraft()
        d.DateOfBirth = New Date(2012, 4, 1)
        d.JoinDate = New Date(2026, 4, 1)
        Assert.Empty(EmployeeValidator.Validate(d, Today))
    End Sub

    <Fact>
    Public Sub BirthDateToday_Fails()
        Dim d = ValidDraft()
        d.DateOfBirth = Today
        Assert.Contains("Date of birth must be in the past.", EmployeeValidator.Validate(d, Today))
    End Sub

    <Fact>
    Public Sub BankAccountWithoutIfsc_Fails()
        Dim d = ValidDraft()
        d.Ifsc = ""
        Assert.Contains("Enter both bank account and IFSC, or leave both blank.", EmployeeValidator.Validate(d, Today))
    End Sub

    <Fact>
    Public Sub BadAadhaar_Fails()
        Dim d = ValidDraft()
        d.Aadhaar = "234567890125"
        Assert.Contains("Aadhaar number is not valid.", EmployeeValidator.Validate(d, Today))
    End Sub

    <Fact>
    Public Sub UnknownPayType_Fails()
        Dim d = ValidDraft()
        d.PayType = CType(99, PayType)
        Assert.Contains("Choose daily wage or monthly salary.", EmployeeValidator.Validate(d, Today))
    End Sub

    <Fact>
    Public Sub NullDraft_Throws()
        Assert.Throws(Of ArgumentNullException)(Function() EmployeeValidator.Validate(Nothing, Today))
    End Sub
End Class
