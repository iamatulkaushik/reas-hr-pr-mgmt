Imports Revolution.Core
Imports Revolution.Modules.Employees
Imports Xunit

Public Class EmployeeStatutoryServiceTests
    Private ReadOnly _repo As New FakeFlagsRepository()
    Private ReadOnly _perms As New FakePermissions()
    Private ReadOnly _audit As New FakeAudit()
    Private ReadOnly _service As EmployeeStatutoryService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Edit)
        _service = New EmployeeStatutoryService(_repo, _perms, _audit, New FakeSession(), Function() New Date(2026, 10, 12))
    End Sub

    <Fact>
    Public Sub GetFlags_NoRow_ReturnsTheDatabaseDefaults()
        Dim f = _service.GetFlags(101).Value
        Assert.False(f.IsEpfMember)
        Assert.False(f.IsEsiMember)
        Assert.True(f.IsPensionMember)
        Assert.True(f.PtApplicable)
        Assert.True(f.LwfApplicable)
        Assert.Equal(2, f.TaxRegimeId)
    End Sub

    <Fact>
    Public Sub SaveFlags_Valid_StoresAndAudits()
        Dim f As New StatutoryFlags With {.IsEpfMember = True, .PfNumber = "HR/GGN/1234567/000/0000123", .EpfJoinDate = New Date(2026, 4, 1), .IsEsiMember = True}
        Assert.True(_service.SaveFlags(101, f).Succeeded)
        Assert.True(_repo.Rows(101).IsEpfMember)
        Assert.Equal("EPF yes, ESI yes, PT yes, LWF yes", _audit.Entries.Single().Details)
    End Sub

    <Fact>
    Public Sub SaveFlags_HigherWageWithoutEpf_IsRefused()
        Dim r = _service.SaveFlags(101, New StatutoryFlags With {.HigherWageOption = True})
        Assert.False(r.Succeeded)
        Assert.Contains("EPF", r.Message)
        Assert.Empty(_repo.Rows)
    End Sub

    <Fact>
    Public Sub SaveFlags_HigherWageWithEpf_IsAccepted()
        Assert.True(_service.SaveFlags(101, New StatutoryFlags With {.IsEpfMember = True, .HigherWageOption = True}).Succeeded)
    End Sub

    <Theory>
    <InlineData(0)>
    <InlineData(3)>
    Public Sub SaveFlags_UnknownTaxRegime_IsRefused(regime As Integer)
        Assert.False(_service.SaveFlags(101, New StatutoryFlags With {.TaxRegimeId = regime}).Succeeded)
    End Sub

    <Fact>
    Public Sub SaveFlags_FutureEpfJoinDate_IsRefused()
        Assert.False(_service.SaveFlags(101, New StatutoryFlags With {.IsEpfMember = True, .EpfJoinDate = New Date(2026, 10, 13)}).Succeeded)
        Assert.True(_service.SaveFlags(101, New StatutoryFlags With {.IsEpfMember = True, .EpfJoinDate = New Date(2026, 10, 12)}).Succeeded)
    End Sub

    <Fact>
    Public Sub SaveFlags_TooLongText_IsRefused()
        Assert.False(_service.SaveFlags(101, New StatutoryFlags With {.PfNumber = New String("x"c, 31)}).Succeeded)
        Assert.False(_service.SaveFlags(101, New StatutoryFlags With {.EsiDispensary = New String("x"c, 101)}).Succeeded)
    End Sub

    <Fact>
    Public Sub SaveFlags_WithoutEditPermission_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.SaveFlags(101, New StatutoryFlags()).Succeeded)
        Assert.Empty(_repo.Rows)
    End Sub

    <Fact>
    Public Sub SaveFlags_UnknownEmployee_ReturnsTheDatabaseMessage()
        _repo.MissingEmployee = True
        Dim r = _service.SaveFlags(101, New StatutoryFlags())
        Assert.False(r.Succeeded)
        Assert.Equal("Employee not found.", r.Message)
    End Sub

    <Fact>
    Public Sub GetFlags_WithoutViewPermission_IsRefused()
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.GetFlags(101).Succeeded)
    End Sub
End Class
