Imports Revolution.Core
Imports Revolution.Modules.Biometric
Imports Xunit

Public Class AgentKeyServiceTests
    Private ReadOnly _repo As New FakeAgentKeyRepository()
    Private ReadOnly _perms As New FakeBioPermissions()
    Private ReadOnly _plan As New FakeBioEntitlements()
    Private ReadOnly _audit As New FakeBioAudit()
    Private ReadOnly _service As AgentKeyService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Create)
        _perms.Allowed.Add(PermissionAction.Edit)
        _service = New AgentKeyService(_repo, _perms, _plan, _audit)
    End Sub

    <Fact>
    Public Sub Issue_GivesTheKeyOnce_AndAuditsWithoutIt()
        Dim r As OperationResult(Of IssuedAgentKey) = _service.Issue(" Gate PC ")
        Assert.True(r.Succeeded)
        Assert.Equal("Gate PC", _repo.Issued.Single())
        Assert.Equal(71, r.Value.RegistrationId)
        Assert.False(String.IsNullOrEmpty(r.Value.Key))
        Dim entry As AuditEntry = _audit.Entries.Single()
        Assert.Equal("AgentKey.Issue", entry.Action)
        Assert.DoesNotContain(r.Value.Key, entry.Details, StringComparison.Ordinal)
        Assert.DoesNotContain("Gate PC", entry.Details, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub Issue_NeedsCreate_AndAGoodLabel()
        Assert.False(_service.Issue(" ").Succeeded)
        Assert.False(_service.Issue(New String("x"c, 61)).Succeeded)
        Assert.True(_service.Issue(New String("x"c, 60)).Succeeded)
        _perms.Allowed.Remove(PermissionAction.Create)
        Assert.False(_service.Issue("Another").Succeeded)
        Assert.Single(_repo.Issued)
    End Sub

    <Fact>
    Public Sub Issue_DatabaseRefusal_IsAFailedResult_WithNothingAudited()
        _repo.FailWith = "Only the Owner or the Associate of this company can manage agent keys."
        Dim r As OperationResult(Of IssuedAgentKey) = _service.Issue("Gate PC")
        Assert.False(r.Succeeded)
        Assert.Contains("Owner or the Associate", r.Message, StringComparison.Ordinal)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub List_NeedsView_AndThePlan()
        _repo.Items.Add(New AgentKeyItem(1, "Gate PC", True, New Date(2026, 11, 8), Nothing, Nothing))
        Assert.Equal(1, _service.List().Value.Count)
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.List().Succeeded)
        _perms.Allowed.Add(PermissionAction.View)
        _plan.BiometricEnabled = False
        Assert.False(_service.List().Succeeded)
        Assert.False(_service.Issue("Gate PC").Succeeded)
    End Sub

    <Fact>
    Public Sub Retire_NeedsEdit_AndAPick_AndAudits()
        Assert.False(_service.Retire(0).Succeeded)
        Assert.True(_service.Retire(5).Succeeded)
        Assert.Equal(5, _repo.Retired.Single())
        Assert.Equal("AgentKey.Retire", _audit.Entries.Single().Action)
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.Retire(6).Succeeded)
    End Sub

    <Fact>
    Public Sub Retire_UnknownAgent_IsAFailedResult()
        _repo.FailWith = "No such agent in this company."
        Assert.False(_service.Retire(5).Succeeded)
    End Sub
End Class
