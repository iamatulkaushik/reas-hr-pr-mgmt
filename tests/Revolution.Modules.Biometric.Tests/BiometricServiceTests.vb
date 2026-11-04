Imports Revolution.Core
Imports Revolution.Modules.Biometric
Imports Xunit

Public Class BiometricServiceTests
    Private ReadOnly _repo As New FakeBiometricRepository()
    Private ReadOnly _perms As New FakeBioPermissions()
    Private ReadOnly _plan As New FakeBioEntitlements()
    Private ReadOnly _audit As New FakeBioAudit()
    Private ReadOnly _service As BiometricService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Create)
        _perms.Allowed.Add(PermissionAction.Edit)
        _service = New BiometricService(_repo, _perms, _plan, _audit, New FakeBioSession())
    End Sub

    Private Shared Function NewDevice() As DeviceWrite
        Return New DeviceWrite With {.FactoryId = 2, .Code = "GATE-1", .Name = "Main gate", .Brand = "ESSL", .Host = "192.168.1.50", .Port = 4370}
    End Function

    <Fact>
    Public Sub PlanWithoutBiometric_RefusesEverything()
        _plan.BiometricEnabled = False
        Assert.False(_service.ListDevices(False).Succeeded)
        Assert.False(_service.SaveDevice(NewDevice()).Succeeded)
        Assert.False(_service.MapCard(5, "ABC123").Succeeded)
        Assert.False(_service.ListPunches(New PunchFilter With {.FromDate = Date.Today, .ToDate = Date.Today}).Succeeded)
        Assert.Empty(_repo.Saved)
    End Sub

    <Fact>
    Public Sub AddDevice_NeedsCreate_ChangeNeedsEdit()
        _perms.Allowed.Remove(PermissionAction.Create)
        Assert.False(_service.SaveDevice(NewDevice()).Succeeded)
        Dim change As DeviceWrite = NewDevice()
        change.DeviceId = 9
        Assert.True(_service.SaveDevice(change).Succeeded)
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.SaveDevice(change).Succeeded)
    End Sub

    <Fact>
    Public Sub SaveDevice_TrimsAndAudits()
        Dim d As DeviceWrite = NewDevice()
        d.Code = " GATE-1 "
        d.Host = " 192.168.1.50 "
        Dim r As OperationResult(Of Integer) = _service.SaveDevice(d)
        Assert.True(r.Succeeded)
        Assert.Equal("GATE-1", _repo.Saved.Single().Code)
        Assert.Equal("192.168.1.50", _repo.Saved.Single().Host)
        Assert.Equal("Device.Add", _audit.Entries.Single().Action)
        Assert.DoesNotContain("192.168", _audit.Entries.Single().Details, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub SaveDevice_BadValues_AreRefused()
        Dim d As DeviceWrite = NewDevice()
        d.FactoryId = 0
        Assert.False(_service.SaveDevice(d).Succeeded)
        d = NewDevice()
        d.Code = "gate 1"
        Assert.False(_service.SaveDevice(d).Succeeded)
        d = NewDevice()
        d.Port = 70000
        Assert.False(_service.SaveDevice(d).Succeeded)
        d = NewDevice()
        d.Host = " "
        Assert.False(_service.SaveDevice(d).Succeeded)
        d = NewDevice()
        d.Brand = ""
        Assert.False(_service.SaveDevice(d).Succeeded)
        Assert.False(_service.SaveDevice(Nothing).Succeeded)
        Assert.Empty(_repo.Saved)
    End Sub

    <Fact>
    Public Sub SaveDevice_DatabaseRule_BecomesAFailedResult()
        _repo.FailWith = "The device code is already used."
        Dim r As OperationResult(Of Integer) = _service.SaveDevice(NewDevice())
        Assert.False(r.Succeeded)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub MapCard_NeedsCreate_ChecksTheNumber_AndAuditsWithoutIt()
        Assert.False(_service.MapCard(0, "ABC123").Succeeded)
        Assert.False(_service.MapCard(5, " ").Succeeded)
        Assert.False(_service.MapCard(5, "12 34").Succeeded)
        Assert.False(_service.MapCard(5, New String("9"c, 31)).Succeeded)
        Assert.True(_service.MapCard(5, " 0A1B2C ").Succeeded)
        Assert.Equal("0A1B2C", _repo.Mapped.Single().Item2)
        Assert.Equal(7, _repo.Mapped.Single().Item3.Value)
        Assert.DoesNotContain("0A1B2C", _audit.Entries.Single().Details, StringComparison.Ordinal)
        _perms.Allowed.Remove(PermissionAction.Create)
        Assert.False(_service.MapCard(5, "ZZZ").Succeeded)
    End Sub

    <Fact>
    Public Sub MapCard_CardOnAnotherEmployee_IsRefused()
        _repo.FailWith = "The card is already mapped to another employee."
        Dim r As OperationResult = _service.MapCard(5, "ABC123")
        Assert.False(r.Succeeded)
        Assert.Contains("another employee", r.Message, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub UnmapCard_NeedsEdit()
        Assert.True(_service.UnmapCard(5).Succeeded)
        Assert.Equal(5, _repo.Unmapped.Single())
        Assert.Equal("Card.Unmap", _audit.Entries.Single().Action)
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.UnmapCard(5).Succeeded)
        Assert.False(_service.UnmapCard(0).Succeeded)
    End Sub

    <Fact>
    Public Sub ListPunches_NeedsView_AndAtMostAMonth()
        Dim from As New Date(2026, 10, 1)
        Assert.True(_service.ListPunches(New PunchFilter With {.FromDate = from, .ToDate = from.AddDays(30), .UnmatchedOnly = True}).Succeeded)
        Assert.True(_repo.LastFilter.UnmatchedOnly)
        Assert.False(_service.ListPunches(New PunchFilter With {.FromDate = from, .ToDate = from.AddDays(31)}).Succeeded)
        Assert.False(_service.ListPunches(New PunchFilter With {.FromDate = from, .ToDate = from.AddDays(-1)}).Succeeded)
        Assert.False(_service.ListPunches(Nothing).Succeeded)
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.ListPunches(New PunchFilter With {.FromDate = from, .ToDate = from}).Succeeded)
    End Sub

    <Fact>
    Public Sub ResolvePunches_NeedsEdit_AndAuditsCounts()
        Dim r As OperationResult(Of ResolveResult) = _service.ResolvePunches()
        Assert.True(r.Succeeded)
        Assert.Equal(3, r.Value.Resolved)
        Assert.Equal("3 matched, 1 waiting.", _audit.Entries.Single().Details)
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.ResolvePunches().Succeeded)
    End Sub

    <Fact>
    Public Sub ListDevicesAndCards_NeedView()
        Assert.True(_service.ListDevices(False).Succeeded)
        Assert.True(_service.ListCards(Nothing, False).Succeeded)
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.ListDevices(False).Succeeded)
        Assert.False(_service.ListCards(Nothing, False).Succeeded)
    End Sub
End Class
