Imports Revolution.Core
Imports Revolution.Modules.Biometric
Imports Xunit

''' <summary>Biometric users are managed apart from devices: an id or card seen on any device is matched to an employee (Owner, 2026-11-06).</summary>
Public Class BiometricUserTests
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
        _repo.Users.Add(New BiometricUserItem(1, "E001", "Ram Kumar", 12, "0A1B", New Date(2026, 11, 5, 8, 1, 0), 40))
        _repo.Users.Add(New BiometricUserItem(2, "E002", "Sita Devi", Nothing, Nothing, Nothing, 0))
        _repo.Users.Add(New BiometricUserItem(3, "E003", "Mohan Lal", Nothing, "9F9F", Nothing, 0))
    End Sub

    <Fact>
    Public Sub ListUsers_ShowsEveryoneOrOnlyThoseNotEnrolled()
        Assert.Equal(3, _service.ListUsers(False).Value.Count)
        Dim waiting As IReadOnlyList(Of BiometricUserItem) = _service.ListUsers(True).Value
        Assert.Equal("E002", waiting.Single().EmpCode)
        Assert.True(_repo.Users(0).IsEnrolled)
        Assert.True(_repo.Users(2).IsEnrolled)
        Assert.False(_repo.Users(1).IsEnrolled)
    End Sub

    <Fact>
    Public Sub ListsAndNextId_NeedView_AndThePlan()
        Assert.Equal(41, _service.NextBiometricId().Value)
        Assert.True(_service.ListUnknownUsers().Succeeded)
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.ListUsers(False).Succeeded)
        Assert.False(_service.ListUnknownUsers().Succeeded)
        Assert.False(_service.NextBiometricId().Succeeded)
        _perms.Allowed.Add(PermissionAction.View)
        _plan.BiometricEnabled = False
        Assert.False(_service.ListUsers(False).Succeeded)
    End Sub

    <Fact>
    Public Sub AssignBiometricId_NeedsEdit_ChecksValues_AndAudits()
        Assert.False(_service.AssignBiometricId(0, 5).Succeeded)
        Assert.False(_service.AssignBiometricId(2, 0).Succeeded)
        Assert.False(_service.AssignBiometricId(2, -3).Succeeded)
        Assert.True(_service.AssignBiometricId(2, 41).Succeeded)
        Assert.Equal(41, _repo.Assigned.Single().Item2.Value)
        Assert.Equal(7, _repo.Assigned.Single().Item3.Value)
        Dim entry As AuditEntry = _audit.Entries.Single()
        Assert.Equal("BiometricUser.Assign", entry.Action)
        Assert.DoesNotContain("41", entry.Details, StringComparison.Ordinal)
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.AssignBiometricId(2, 42).Succeeded)
    End Sub

    <Fact>
    Public Sub AssignBiometricId_IdOfAnotherEmployee_IsRefused()
        _repo.FailWith = "The biometric id belongs to another employee."
        Dim r As OperationResult = _service.AssignBiometricId(2, 12)
        Assert.False(r.Succeeded)
        Assert.Contains("another employee", r.Message, StringComparison.Ordinal)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub ClearBiometricId_SendsNothing_AndNeedsEdit()
        Assert.True(_service.ClearBiometricId(1).Succeeded)
        Assert.False(_repo.Assigned.Single().Item2.HasValue)
        Assert.Equal("BiometricUser.Clear", _audit.Entries.Single().Action)
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.ClearBiometricId(1).Succeeded)
        Assert.False(_service.ClearBiometricId(0).Succeeded)
    End Sub

    <Fact>
    Public Sub LinkUnknown_ById_AssignsThenMatchesWaitingPunches()
        Dim unknown As New UnknownBiometricUser(77, Nothing, 12, New Date(2026, 11, 1), New Date(2026, 11, 5), 2)
        Dim r As OperationResult(Of ResolveResult) = _service.LinkUnknown(unknown, 2)
        Assert.True(r.Succeeded)
        Assert.Equal(77, _repo.Assigned.Single().Item2.Value)
        Assert.Equal(1, _repo.ResolveCalls)
        Assert.Equal(3, r.Value.Resolved)
    End Sub

    <Fact>
    Public Sub LinkUnknown_ByCard_MapsTheCardThenMatches()
        Dim unknown As New UnknownBiometricUser(Nothing, "ABC999", 4, New Date(2026, 11, 1), New Date(2026, 11, 5), 1)
        Assert.True(_service.LinkUnknown(unknown, 2).Succeeded)
        Assert.Equal("ABC999", _repo.Mapped.Single().Item2)
        Assert.Empty(_repo.Assigned)
        Assert.Equal(1, _repo.ResolveCalls)
    End Sub

    <Fact>
    Public Sub LinkUnknown_Failure_DoesNotMatchAnything()
        _repo.FailWith = "The biometric id belongs to another employee."
        Dim unknown As New UnknownBiometricUser(12, Nothing, 4, New Date(2026, 11, 1), New Date(2026, 11, 5), 1)
        Assert.False(_service.LinkUnknown(unknown, 2).Succeeded)
        Assert.Equal(0, _repo.ResolveCalls)
        Assert.False(_service.LinkUnknown(Nothing, 2).Succeeded)
        Assert.False(_service.LinkUnknown(New UnknownBiometricUser(Nothing, " ", 1, New Date(2026, 11, 1), New Date(2026, 11, 1), 1), 2).Succeeded)
    End Sub
End Class
