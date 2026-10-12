Imports System.Security.Cryptography
Imports System.Text
Imports Revolution.Core
Imports Revolution.Modules.Employees
Imports Xunit

Public Class EmployeeIdentifierServiceTests
    Private Const CompanyId As Integer = 3
    Private Const EmployeeId As Integer = 101

    Private ReadOnly _repo As New FakeEmployeeRepository()
    Private ReadOnly _crypto As New FakeFieldCrypto()
    Private ReadOnly _perms As New FakePermissions()
    Private ReadOnly _audit As New FakeAudit()
    Private ReadOnly _service As EmployeeIdentifierService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Edit)
        _service = New EmployeeIdentifierService(_repo, _crypto, _perms, _audit, New FakeSession())
    End Sub

    Private Shared Function PanOnly(pan As String) As IdentifierInput
        Return New IdentifierInput With {.Pan = IdEdit.ReplaceWith(pan)}
    End Function

    Private Function Plain(cipher As Byte(), field As String) As String
        Return _crypto.Decrypt(cipher, _repo.Ciphers(EmployeeId).KeyVersion, FieldContext.Build(CompanyId, EmployeeId, field))
    End Function

    <Fact>
    Public Sub Save_Replace_StoresCipherHashAndLast4_NeverPlain()
        Assert.True(_service.Save(EmployeeId, PanOnly("ABCPE1234F")).Succeeded)
        Dim written = _repo.Writes(0).Pan.Value
        Assert.Equal("234F", written.Last4)
        Assert.Equal(32, written.Hash.Length)
        Assert.DoesNotContain("ABCPE1234F", Encoding.UTF8.GetString(written.Hash))
        Assert.Equal("ABCPE1234F", Plain(_repo.Ciphers(EmployeeId).Pan, FieldContext.Pan))
        Assert.Equal(IdChangeKind.Unchanged, _repo.Writes(0).Uan.Kind)
    End Sub

    <Fact>
    Public Sub Save_BindsCipherToCompanyEmployeeAndField()
        _service.Save(EmployeeId, PanOnly("ABCPE1234F"))
        Dim cipher = _repo.Ciphers(EmployeeId).Pan
        Assert.Throws(Of CryptographicException)(Function() _crypto.Decrypt(cipher, 1, FieldContext.Build(CompanyId, 999, FieldContext.Pan)))
        Assert.Throws(Of CryptographicException)(Function() _crypto.Decrypt(cipher, 1, FieldContext.Build(CompanyId, EmployeeId, FieldContext.Aadhaar)))
    End Sub

    <Fact>
    Public Sub Save_NormalizesSpacesHyphensAndCase()
        _service.Save(EmployeeId, New IdentifierInput With {.Aadhaar = IdEdit.ReplaceWith("2345 6789-0124"), .Pan = IdEdit.ReplaceWith("abcpe1234f")})
        Assert.Equal("234567890124", Plain(_repo.Ciphers(EmployeeId).Aadhaar, FieldContext.Aadhaar))
        Assert.Equal("ABCPE1234F", Plain(_repo.Ciphers(EmployeeId).Pan, FieldContext.Pan))
    End Sub

    <Fact>
    Public Sub Save_InvalidId_IsRefusedBeforeAnyWrite()
        Dim r = _service.Save(EmployeeId, PanOnly("NOTAPAN"))
        Assert.False(r.Succeeded)
        Assert.Empty(_repo.Writes)
    End Sub

    <Fact>
    Public Sub Save_NothingChanged_WritesNothing()
        Assert.True(_service.Save(EmployeeId, New IdentifierInput()).Succeeded)
        Assert.Empty(_repo.Writes)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub Save_Clear_RemovesTheId()
        _service.Save(EmployeeId, PanOnly("ABCPE1234F"))
        _service.Save(EmployeeId, New IdentifierInput With {.Pan = IdEdit.Cleared})
        Assert.Null(_repo.Ciphers(EmployeeId).Pan)
    End Sub

    <Fact>
    Public Sub Save_WithoutEditPermission_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.Save(EmployeeId, PanOnly("ABCPE1234F")).Succeeded)
        Assert.Empty(_repo.Writes)
    End Sub

    <Fact>
    Public Sub Save_DatabaseRefusesDuplicate_ReturnsItsMessage()
        _repo.Hashes(Convert.ToHexString(_crypto.BlindIndex(CompanyId, FieldContext.Pan, "ABCPE1234F"))) = 555
        Dim r = _service.Save(EmployeeId, PanOnly("ABCPE1234F"))
        Assert.False(r.Succeeded)
        Assert.Contains("another employee", r.Message)
    End Sub

    <Fact>
    Public Sub Save_AuditNamesFieldsOnly_NeverValues()
        _service.Save(EmployeeId, New IdentifierInput With {.Pan = IdEdit.ReplaceWith("ABCPE1234F"), .Uan = IdEdit.ReplaceWith("100123456789")})
        Dim entry = _audit.Entries.Single()
        Assert.Equal("Changed: UAN, PAN", entry.Details)
        Assert.DoesNotContain("ABCPE1234F", entry.Details)
        Assert.DoesNotContain("100123456789", entry.Details)
    End Sub

    <Fact>
    Public Sub Save_AfterKeyRotation_ReEncryptsTheWholeRowUnderTheNewVersion()
        _service.Save(EmployeeId, New IdentifierInput With {.Pan = IdEdit.ReplaceWith("ABCPE1234F"), .Uan = IdEdit.ReplaceWith("100123456789")})
        _crypto.Version = 2
        Assert.True(_service.Save(EmployeeId, New IdentifierInput With {.Aadhaar = IdEdit.ReplaceWith("234567890124")}).Succeeded)
        Assert.Equal(CByte(2), _repo.Ciphers(EmployeeId).KeyVersion)
        Assert.Equal("ABCPE1234F", Plain(_repo.Ciphers(EmployeeId).Pan, FieldContext.Pan))
        Assert.Equal("100123456789", Plain(_repo.Ciphers(EmployeeId).Uan, FieldContext.Uan))
        Assert.Equal("234567890124", Plain(_repo.Ciphers(EmployeeId).Aadhaar, FieldContext.Aadhaar))
    End Sub

    <Fact>
    Public Sub Save_SameKeyVersion_LeavesOtherIdsAlone()
        _service.Save(EmployeeId, PanOnly("ABCPE1234F"))
        _service.Save(EmployeeId, New IdentifierInput With {.Uan = IdEdit.ReplaceWith("100123456789")})
        Assert.Equal(IdChangeKind.Unchanged, _repo.Writes(1).Pan.Kind)
    End Sub

    <Fact>
    Public Sub Save_UnreadableStoredId_FailsWithoutDetail()
        _service.Save(EmployeeId, PanOnly("ABCPE1234F"))
        _crypto.Version = 2
        _crypto.FailDecrypt = True
        Dim r = _service.Save(EmployeeId, New IdentifierInput With {.Uan = IdEdit.ReplaceWith("100123456789")})
        Assert.False(r.Succeeded)
        Assert.DoesNotContain("ABCPE1234F", r.Message)
    End Sub

    <Fact>
    Public Sub Reveal_WithoutRevealPermission_IsRefusedAndNotLogged()
        _service.Save(EmployeeId, PanOnly("ABCPE1234F"))
        Assert.False(_service.Reveal(EmployeeId, IdField.Pan).Succeeded)
        Assert.Empty(_repo.RevealLog)
    End Sub

    <Fact>
    Public Sub Reveal_Allowed_ReturnsPlainAndLogsFieldNamesOnly()
        _service.Save(EmployeeId, New IdentifierInput With {.Pan = IdEdit.ReplaceWith("ABCPE1234F"), .Uan = IdEdit.ReplaceWith("100123456789")})
        _perms.Allowed.Add(PermissionAction.Reveal)
        Dim r = _service.Reveal(EmployeeId, IdField.Pan Or IdField.Uan)
        Assert.True(r.Succeeded)
        Assert.Equal("ABCPE1234F", r.Value.Pan)
        Assert.Equal("100123456789", r.Value.Uan)
        Assert.Equal(String.Empty, r.Value.Aadhaar)
        Assert.Equal("7:101:UAN,PAN", _repo.RevealLog.Single())
    End Sub

    <Fact>
    Public Sub Reveal_AfterKeyRotation_StillReadsOldIds()
        _service.Save(EmployeeId, PanOnly("ABCPE1234F"))
        _crypto.Version = 2
        _perms.Allowed.Add(PermissionAction.Reveal)
        Assert.Equal("ABCPE1234F", _service.Reveal(EmployeeId, IdField.Pan).Value.Pan)
    End Sub

    <Fact>
    Public Sub Reveal_NothingOnFile_ReturnsEmptyAndDoesNotLog()
        _perms.Allowed.Add(PermissionAction.Reveal)
        Dim r = _service.Reveal(EmployeeId, IdField.All)
        Assert.True(r.Succeeded)
        Assert.False(r.Value.Any)
        Assert.Empty(_repo.RevealLog)
    End Sub

    <Fact>
    Public Sub Reveal_WrongKey_FailsWithoutDetailAndDoesNotLog()
        _service.Save(EmployeeId, PanOnly("ABCPE1234F"))
        _perms.Allowed.Add(PermissionAction.Reveal)
        _crypto.FailDecrypt = True
        Dim r = _service.Reveal(EmployeeId, IdField.Pan)
        Assert.False(r.Succeeded)
        Assert.DoesNotContain("ABCPE1234F", r.Message)
        Assert.Empty(_repo.RevealLog)
    End Sub

    <Fact>
    Public Sub GetMasked_NoRow_ReturnsEmptyLast4()
        Dim r = _service.GetMasked(EmployeeId)
        Assert.True(r.Succeeded)
        Assert.Equal(String.Empty, r.Value.Pan)
    End Sub
End Class
