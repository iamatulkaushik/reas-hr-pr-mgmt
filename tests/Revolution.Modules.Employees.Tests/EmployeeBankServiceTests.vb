Imports System.Security.Cryptography
Imports Revolution.Core
Imports Revolution.Modules.Employees
Imports Xunit

Public Class EmployeeBankServiceTests
    Private Const CompanyId As Integer = 3
    Private Const EmployeeId As Integer = 101

    Private ReadOnly _repo As New FakeBankRepository()
    Private ReadOnly _crypto As New FakeFieldCrypto()
    Private ReadOnly _perms As New FakePermissions()
    Private ReadOnly _audit As New FakeAudit()
    Private ReadOnly _service As EmployeeBankService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Edit)
        _service = New EmployeeBankService(_repo, _crypto, _perms, _audit, New FakeSession())
    End Sub

    Private Shared Function Transfer(account As String, Optional ifsc As String = "SBIN0001234") As BankDetails
        Return New BankDetails With {.PaymentMode = PaymentMode.BankTransfer, .Ifsc = ifsc, .Account = IdEdit.FromText(account)}
    End Function

    <Fact>
    Public Sub Save_BankTransfer_StoresEncryptedAccountAndLast4_NeverPlain()
        Assert.True(_service.Save(EmployeeId, Transfer("123456789012")).Succeeded)
        Dim write = _repo.Writes.Single()
        Assert.Equal("9012", write.Account.Value.Last4)
        Assert.Null(write.Account.Value.Hash)
        Assert.Equal("123456789012", _crypto.Decrypt(write.Account.Value.Cipher, 1, FieldContext.Build(CompanyId, EmployeeId, FieldContext.BankAccount)))
    End Sub

    <Fact>
    Public Sub Save_BindsCipherToEmployeeAndField()
        _service.Save(EmployeeId, Transfer("123456789012"))
        Dim cipher = _repo.Writes.Single().Account.Value.Cipher
        Assert.Throws(Of CryptographicException)(Function() _crypto.Decrypt(cipher, 1, FieldContext.Build(CompanyId, 999, FieldContext.BankAccount)))
        Assert.Throws(Of CryptographicException)(Function() _crypto.Decrypt(cipher, 1, FieldContext.Build(CompanyId, EmployeeId, FieldContext.Pan)))
    End Sub

    <Fact>
    Public Sub Save_NormalizesAccountAndIfsc()
        _service.Save(EmployeeId, Transfer("1234 5678-9012", "sbin 0001234"))
        Dim write = _repo.Writes.Single()
        Assert.Equal("SBIN0001234", write.Ifsc)
        Assert.Equal("123456789012", _crypto.Decrypt(write.Account.Value.Cipher, 1, FieldContext.Build(CompanyId, EmployeeId, FieldContext.BankAccount)))
    End Sub

    <Fact>
    Public Sub Save_BankTransferWithoutAccountOrIfsc_ListsBothProblems()
        Dim r = _service.Save(EmployeeId, New BankDetails With {.PaymentMode = PaymentMode.BankTransfer})
        Assert.False(r.Succeeded)
        Assert.Contains("account number", r.Message)
        Assert.Contains("IFSC", r.Message)
        Assert.Empty(_repo.Writes)
    End Sub

    <Fact>
    Public Sub Save_ExistingAccountKept_WhenOnlyIfscChanges()
        _service.Save(EmployeeId, Transfer("123456789012"))
        Dim r = _service.Save(EmployeeId, Transfer("", "HDFC0000123"))
        Assert.True(r.Succeeded)
        Assert.Equal(IdChangeKind.Unchanged, _repo.Writes(1).Account.Kind)
        Assert.Equal("9012", _repo.Banks(EmployeeId).AccountLast4)
        Assert.Equal("HDFC0000123", _repo.Banks(EmployeeId).Ifsc)
    End Sub

    <Fact>
    Public Sub Save_ClearingTheAccountOnBankTransfer_IsRefused()
        _service.Save(EmployeeId, Transfer("123456789012"))
        Dim d = Transfer("")
        d.Account = IdEdit.Cleared
        Assert.False(_service.Save(EmployeeId, d).Succeeded)
        Assert.Single(_repo.Writes)
    End Sub

    <Fact>
    Public Sub Save_CashNeedsNoAccountOrIfsc_AndCanClearIt()
        _service.Save(EmployeeId, Transfer("123456789012"))
        Dim cash As New BankDetails With {.PaymentMode = PaymentMode.Cash, .Account = IdEdit.Cleared}
        Assert.True(_service.Save(EmployeeId, cash).Succeeded)
        Assert.False(_repo.Banks(EmployeeId).HasAccount)
        Assert.Null(_repo.Accounts(EmployeeId).Cipher)
    End Sub

    <Theory>
    <InlineData("12345678")>
    <InlineData("1234567890123456789")>
    <InlineData("12345678901A")>
    Public Sub Save_BadAccount_IsRefused(account As String)
        Assert.False(_service.Save(EmployeeId, Transfer(account)).Succeeded)
        Assert.Empty(_repo.Writes)
    End Sub

    <Fact>
    Public Sub Save_BadIfsc_IsRefused()
        Dim r = _service.Save(EmployeeId, Transfer("123456789012", "SBIN1001234"))
        Assert.False(r.Succeeded)
        Assert.Contains("IFSC", r.Message)
    End Sub

    <Fact>
    Public Sub Save_UnknownPaymentMode_IsRefused()
        Dim d = Transfer("123456789012")
        d.PaymentMode = CType(9, PaymentMode)
        Assert.False(_service.Save(EmployeeId, d).Succeeded)
    End Sub

    <Fact>
    Public Sub Save_WithoutEditPermission_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.Save(EmployeeId, Transfer("123456789012")).Succeeded)
        Assert.Empty(_repo.Writes)
    End Sub

    <Fact>
    Public Sub Save_UnknownEmployee_ReturnsTheDatabaseMessage()
        _repo.MissingEmployee = True
        Dim r = _service.Save(EmployeeId, Transfer("123456789012"))
        Assert.False(r.Succeeded)
        Assert.Equal("Employee not found.", r.Message)
    End Sub

    <Fact>
    Public Sub Save_AuditHoldsNoNumberAndNoIfsc()
        _service.Save(EmployeeId, Transfer("123456789012"))
        Dim details = _audit.Entries.Single().Details
        Assert.Equal("Mode BankTransfer; account changed", details)
        Assert.DoesNotContain("123456789012", details)
        Assert.DoesNotContain("9012", details)
    End Sub

    <Fact>
    Public Sub Save_NewAccountAfterKeyRotation_UsesTheNewVersion()
        _service.Save(EmployeeId, Transfer("123456789012"))
        _crypto.Version = 2
        _service.Save(EmployeeId, Transfer("987654321098"))
        Assert.Equal(CByte(2), _repo.Accounts(EmployeeId).KeyVersion)
    End Sub

    <Fact>
    Public Sub GetMasked_NoRow_ReturnsDefaults()
        Dim r = _service.GetMasked(EmployeeId)
        Assert.True(r.Succeeded)
        Assert.False(r.Value.HasAccount)
    End Sub

    <Fact>
    Public Sub Reveal_WithoutPermission_IsRefusedAndNotLogged()
        _service.Save(EmployeeId, Transfer("123456789012"))
        Assert.False(_service.RevealAccount(EmployeeId).Succeeded)
        Assert.Empty(_repo.RevealLog)
    End Sub

    <Fact>
    Public Sub Reveal_Allowed_ReturnsPlainAndLogsTheFieldNameOnly()
        _service.Save(EmployeeId, Transfer("123456789012"))
        _perms.Allowed.Add(PermissionAction.Reveal)
        Dim r = _service.RevealAccount(EmployeeId)
        Assert.True(r.Succeeded)
        Assert.Equal("123456789012", r.Value)
        Assert.Equal("7:101:BANKACCOUNT", _repo.RevealLog.Single())
    End Sub

    <Fact>
    Public Sub Reveal_AfterKeyRotation_StillReadsTheOldAccount()
        _service.Save(EmployeeId, Transfer("123456789012"))
        _crypto.Version = 2
        _perms.Allowed.Add(PermissionAction.Reveal)
        Assert.Equal("123456789012", _service.RevealAccount(EmployeeId).Value)
    End Sub

    <Fact>
    Public Sub Reveal_NoAccountOnFile_ReturnsEmptyAndDoesNotLog()
        _perms.Allowed.Add(PermissionAction.Reveal)
        Dim r = _service.RevealAccount(EmployeeId)
        Assert.True(r.Succeeded)
        Assert.Equal(String.Empty, r.Value)
        Assert.Empty(_repo.RevealLog)
    End Sub

    <Fact>
    Public Sub Reveal_WrongKey_FailsWithoutDetailAndDoesNotLog()
        _service.Save(EmployeeId, Transfer("123456789012"))
        _perms.Allowed.Add(PermissionAction.Reveal)
        _crypto.FailDecrypt = True
        Dim r = _service.RevealAccount(EmployeeId)
        Assert.False(r.Succeeded)
        Assert.DoesNotContain("123456789012", r.Message)
        Assert.Empty(_repo.RevealLog)
    End Sub
End Class
