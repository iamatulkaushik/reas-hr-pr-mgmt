Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class PayrollLockTests
    Private ReadOnly _runs As New FakeRunRepository()
    Private ReadOnly _approvals As New FakeApprovals()
    Private ReadOnly _perms As New FakePayPermissions()
    Private ReadOnly _plan As New FakePayEntitlements()
    Private ReadOnly _audit As New FakePayAudit()
    Private ReadOnly _salary As New FakeSalaryRepository()
    Private ReadOnly _directory As New FakeRunDirectory()
    Private ReadOnly _service As PayrollRunService

    Public Sub New()
        For Each action As PermissionAction In [Enum].GetValues(GetType(PermissionAction))
            _perms.Allowed.Add(action)
        Next
        _service = New PayrollRunService(_runs, _salary, New FakeRates(), _approvals, _perms, _plan, _audit, New FakePaySession(), Nothing, Nothing, Nothing, _directory)
    End Sub

    <Fact>
    Public Sub LockPayroll_ProcessedRun_IsApprovedAndLocked_WhenNoSecondApproverIsNeeded()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Processed)
        Assert.True(_service.LockPayroll(id).Succeeded)
        Assert.Equal(PayrollRunStatus.Locked, _runs.StatusOf(id))
        Assert.Contains(_audit.Entries, Function(e) e.Action = "PayrollRun.Approve")
        Assert.Contains(_audit.Entries, Function(e) e.Action = "PayrollRun.Lock")
    End Sub

    <Fact>
    Public Sub LockPayroll_ApprovedRun_IsLocked()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Approved)
        Assert.True(_service.LockPayroll(id).Succeeded)
        Assert.Equal(PayrollRunStatus.Locked, _runs.StatusOf(id))
    End Sub

    <Fact>
    Public Sub LockPayroll_PlanNeedingApproval_RefusesAProcessedRun()
        _approvals.Required = True
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Processed)
        Dim r = _service.LockPayroll(id)
        Assert.False(r.Succeeded)
        Assert.Contains("approved", r.Message, StringComparison.Ordinal)
        Assert.Equal(PayrollRunStatus.Processed, _runs.StatusOf(id))
    End Sub

    <Fact>
    Public Sub LockPayroll_SubmittedRun_IsWaiting()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Submitted)
        Dim r = _service.LockPayroll(id)
        Assert.False(r.Succeeded)
        Assert.Contains("waiting for approval", r.Message, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub LockPayroll_DraftRun_MustBeProcessedFirst()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Draft)
        Dim r = _service.LockPayroll(id)
        Assert.False(r.Succeeded)
        Assert.Contains("Process the run", r.Message, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub LockPayroll_AlreadyLockedOrPaid_IsRefused()
        Dim locked As Integer = _runs.AddRun(PayrollRunStatus.Locked)
        Dim paid As Integer = _runs.AddRun(PayrollRunStatus.Paid)
        Assert.Contains("already locked", _service.LockPayroll(locked).Message, StringComparison.Ordinal)
        Assert.Contains("already locked", _service.LockPayroll(paid).Message, StringComparison.Ordinal)
    End Sub

    <Fact>
    Public Sub LockPayroll_UnknownRun_IsRefused()
        Assert.False(_service.LockPayroll(999).Succeeded)
    End Sub

    <Fact>
    Public Sub LockPayroll_WithoutEdit_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Processed)
        Assert.False(_service.LockPayroll(id).Succeeded)
        Assert.Equal(PayrollRunStatus.Processed, _runs.StatusOf(id))
    End Sub

    <Fact>
    Public Sub LockPayroll_LockFailsAfterApproval_SaysTheRunIsApprovedButNotLocked()
        _runs.FailStatus = PayrollRunStatus.Locked
        _runs.FailStatusMessage = "The totals do not match."
        _runs.FailStatusNumber = 50315
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Processed)
        Dim r = _service.LockPayroll(id)
        Assert.False(r.Succeeded)
        Assert.Contains("approved but not locked", r.Message, StringComparison.Ordinal)
        Assert.Equal(PayrollRunStatus.Approved, _runs.StatusOf(id))
    End Sub

    <Fact>
    Public Sub LockPayroll_LoanRecoveriesFail_KeepsTheLockMessage()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Processed)
        _runs.Lines(id) = New List(Of RunLineResult) From {RunTestData.Line(101, 1000D)}
        _runs.Installments.Add(New EmployeeInstallment(101, New OpenInstallment(1, 1, 202610, 1000D)))
        _runs.FailRecoveryMessage = "Could not save."
        _runs.FailRecoveryNumber = 50327
        Dim r = _service.LockPayroll(id)
        Assert.False(r.Succeeded)
        Assert.Contains("loan recoveries were not saved", r.Message, StringComparison.Ordinal)
        Assert.DoesNotContain("approved but not locked", r.Message)
        Assert.Equal(PayrollRunStatus.Locked, _runs.StatusOf(id))
    End Sub

    <Fact>
    Public Sub ListRunsOfMonth_AsksForThePayPeriod()
        _directory.Runs.Add(New PayrollRunHeader(1, New Date(2026, 10, 1), New Date(2026, 10, 31), 1, PayrollRunStatus.Processed, 5, 1000D, 100D, 900D))
        Dim r = _service.ListRunsOfMonth(2026, 10)
        Assert.True(r.Succeeded)
        Assert.Single(r.Value)
        Assert.Equal(New Date(2026, 10, 1), _directory.LastPeriod.PeriodFrom)
        Assert.Equal(New Date(2026, 10, 31), _directory.LastPeriod.PeriodTo)
    End Sub

    <Fact>
    Public Sub ListRunsOfMonth_FollowsAPayCycleThatStartsOnThe26th()
        _salary.Policy = New PolicySettings(26, 2D, False, False, 26D, 25D)
        Assert.True(_service.ListRunsOfMonth(2026, 10).Succeeded)
        Assert.Equal(New Date(2026, 9, 26), _directory.LastPeriod.PeriodFrom)
    End Sub

    <Fact>
    Public Sub ListRunsOfMonth_BadMonth_WithoutView_OrWithoutDirectory_IsRefused()
        Assert.False(_service.ListRunsOfMonth(2026, 13).Succeeded)
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.ListRunsOfMonth(2026, 10).Succeeded)
        Dim viewOnly As New FakePayPermissions()
        viewOnly.Allowed.Add(PermissionAction.View)
        Dim bare As New PayrollRunService(_runs, _salary, New FakeRates(), _approvals, viewOnly, _plan, _audit, New FakePaySession())
        Assert.False(bare.ListRunsOfMonth(2026, 10).Succeeded)
    End Sub
End Class
