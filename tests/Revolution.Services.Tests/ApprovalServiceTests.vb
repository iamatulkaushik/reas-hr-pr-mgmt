Imports Revolution.Core
Imports Revolution.Services
Imports Xunit

Public Class ApprovalServiceTests
    Private Const RunId As String = "2026-09"

    Private ReadOnly _session As New SessionContext()
    Private ReadOnly _repo As New FakeApprovalRepository()
    Private ReadOnly _audit As New FakeAuditRepository()
    Private ReadOnly _svc As ApprovalService

    Public Sub New()
        Dim grants As New FakePermissionRepository()
        grants.Grant(UserRole.Associate, ModuleKey.Payroll, PermissionAction.Edit)
        grants.Grant(UserRole.Owner, ModuleKey.Payroll, PermissionAction.Edit)
        grants.Grant(UserRole.Owner, ModuleKey.Payroll, PermissionAction.Approve)
        Dim entitlements = New EntitlementService(_session)
        _svc = New ApprovalService(_repo, _session, New PermissionService(_session, grants), entitlements,
                                   New AuditService(_audit, _session, entitlements))
    End Sub

    Private Sub SignInAs(userId As Integer, role As UserRole, Optional plan As PlanTier = PlanTier.Platinum)
        _session.SignIn(New UserSession(userId, "user" & userId, role, 1, plan))
    End Sub

    Private Function SubmitAsAssociate() As Integer
        SignInAs(10, UserRole.Associate)
        Return _svc.Submit(ModuleKey.Payroll, "PayrollRun", RunId, "September payroll").Value
    End Function

    <Fact>
    Public Sub Gold_DoesNotRequireApproval_AndIsAlwaysCleared()
        SignInAs(10, UserRole.Associate, PlanTier.Gold)
        Assert.False(_svc.IsRequired(ModuleKey.Payroll))
        Assert.True(_svc.IsCleared(ModuleKey.Payroll, "PayrollRun", RunId))
        Assert.False(_svc.Submit(ModuleKey.Payroll, "PayrollRun", RunId, "x").Succeeded)
    End Sub

    <Fact>
    Public Sub Platinum_NotClearedUntilApproved()
        Dim id = SubmitAsAssociate()
        Assert.True(_svc.IsRequired(ModuleKey.Payroll))
        Assert.False(_svc.IsCleared(ModuleKey.Payroll, "PayrollRun", RunId))
        SignInAs(20, UserRole.Owner)
        Assert.True(_svc.Approve(id, "ok").Succeeded)
        Assert.True(_svc.IsCleared(ModuleKey.Payroll, "PayrollRun", RunId))
    End Sub

    <Fact>
    Public Sub Submit_NeedsEditPermission()
        SignInAs(30, UserRole.Operator)
        Assert.False(_svc.Submit(ModuleKey.Payroll, "PayrollRun", RunId, "x").Succeeded)
    End Sub

    <Fact>
    Public Sub Submit_NotSignedIn_Fails()
        Assert.False(_svc.Submit(ModuleKey.Payroll, "PayrollRun", RunId, "x").Succeeded)
    End Sub

    <Fact>
    Public Sub Submit_NeedsSummary()
        SignInAs(10, UserRole.Associate)
        Assert.False(_svc.Submit(ModuleKey.Payroll, "PayrollRun", RunId, " ").Succeeded)
        Assert.False(_svc.Submit(ModuleKey.Payroll, "PayrollRun", RunId, New String("x"c, 501)).Succeeded)
    End Sub

    <Fact>
    Public Sub Submit_SecondPendingForSameItem_Fails()
        SubmitAsAssociate()
        Assert.False(_svc.Submit(ModuleKey.Payroll, "PayrollRun", RunId, "again").Succeeded)
    End Sub

    <Fact>
    Public Sub Submitter_CannotApproveOwnRequest_EvenAsOwner()
        SignInAs(20, UserRole.Owner)
        Dim id = _svc.Submit(ModuleKey.Payroll, "PayrollRun", RunId, "Owner prepared").Value
        Assert.False(_svc.Approve(id).Succeeded)
        Assert.False(_svc.IsCleared(ModuleKey.Payroll, "PayrollRun", RunId))
    End Sub

    <Fact>
    Public Sub Associate_CannotApprove()
        Dim id = SubmitAsAssociate()
        SignInAs(11, UserRole.Associate)
        Assert.False(_svc.Approve(id).Succeeded)
    End Sub

    <Fact>
    Public Sub Reject_NeedsReason_AndLeavesItNotCleared()
        Dim id = SubmitAsAssociate()
        SignInAs(20, UserRole.Owner)
        Assert.False(_svc.Reject(id, "no").Succeeded)
        Assert.True(_svc.Reject(id, "Overtime figures look wrong").Succeeded)
        Assert.False(_svc.IsCleared(ModuleKey.Payroll, "PayrollRun", RunId))
        Assert.Equal(ApprovalStatus.Rejected, _svc.GetLatest(ModuleKey.Payroll, "PayrollRun", RunId).Status)
    End Sub

    <Fact>
    Public Sub Decided_RequestCannotBeDecidedAgain()
        Dim id = SubmitAsAssociate()
        SignInAs(20, UserRole.Owner)
        Assert.True(_svc.Approve(id).Succeeded)
        Assert.False(_svc.Approve(id).Succeeded)
        Assert.False(_svc.Reject(id, "Changed my mind later").Succeeded)
    End Sub

    <Fact>
    Public Sub Cancel_OnlyBySubmitter()
        Dim id = SubmitAsAssociate()
        SignInAs(20, UserRole.Owner)
        Assert.False(_svc.Cancel(id).Succeeded)
        SignInAs(10, UserRole.Associate)
        Assert.True(_svc.Cancel(id).Succeeded)
        Assert.False(_svc.IsCleared(ModuleKey.Payroll, "PayrollRun", RunId))
    End Sub

    <Fact>
    Public Sub ResubmitAfterRejection_IsAllowed()
        Dim id = SubmitAsAssociate()
        SignInAs(20, UserRole.Owner)
        _svc.Reject(id, "Overtime figures look wrong")
        SignInAs(10, UserRole.Associate)
        Assert.True(_svc.Submit(ModuleKey.Payroll, "PayrollRun", RunId, "Fixed overtime").Succeeded)
    End Sub

    <Fact>
    Public Sub ListPending_OnlyForApprovers()
        SubmitAsAssociate()
        Assert.Empty(_svc.ListPending(ModuleKey.Payroll))
        SignInAs(20, UserRole.Owner)
        Assert.Single(_svc.ListPending(ModuleKey.Payroll))
    End Sub

    <Fact>
    Public Sub Decisions_AreAudited_OnPlatinum()
        Dim id = SubmitAsAssociate()
        SignInAs(20, UserRole.Owner)
        _svc.Approve(id, "ok")
        Assert.Equal(2, _audit.Entries.Count)
        Assert.Equal("ApprovalApproved", _audit.Entries(1).Action)
    End Sub
End Class
