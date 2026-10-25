Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class PayrollRunServiceTests
    Private ReadOnly _runs As New FakeRunRepository()
    Private ReadOnly _salary As New FakeSalaryRepository()
    Private ReadOnly _rates As New FakeRates()
    Private ReadOnly _approvals As New FakeApprovals()
    Private ReadOnly _perms As New FakePayPermissions()
    Private ReadOnly _plan As New FakePayEntitlements()
    Private ReadOnly _audit As New FakePayAudit()
    Private ReadOnly _service As PayrollRunService

    Public Sub New()
        For Each action As PermissionAction In [Enum].GetValues(GetType(PermissionAction))
            _perms.Allowed.Add(action)
        Next
        _service = New PayrollRunService(_runs, _salary, _rates, _approvals, _perms, _plan, _audit, New FakePaySession())
    End Sub

    Private Sub TwoEmployees()
        _runs.Inputs.Add(RunTestData.Facts(101, "E101", paidDays:=30D))
        _runs.Inputs.Add(RunTestData.Facts(102, "E102", paidDays:=30D))
    End Sub

    <Fact>
    Public Sub CreateRegularRun_FollowsPayCycle()
        _salary.Policy = New PolicySettings(26, 2D, False, False, 26D, 25D)
        Dim r = _service.CreateRegularRun(2026, 10)
        Assert.True(r.Succeeded)
        Assert.Equal(New Date(2026, 9, 26), _runs.Created.PeriodFrom)
        Assert.Equal(New Date(2026, 10, 25), _runs.Created.PeriodTo)
        Assert.Equal("PayrollRun.Create", _audit.Entries(0).Action)
    End Sub

    <Fact>
    Public Sub CreateRegularRun_CalendarMonth()
        Assert.True(_service.CreateRegularRun(2026, 10).Succeeded)
        Assert.Equal(New Date(2026, 10, 31), _runs.Created.PeriodTo)
    End Sub

    <Fact>
    Public Sub CreateRegularRun_WithoutCreate_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Create)
        Assert.False(_service.CreateRegularRun(2026, 10).Succeeded)
    End Sub

    <Fact>
    Public Sub CreateRegularRun_ExistingRun_ShowsDatabaseMessage()
        _runs.FailCreate = "A run already exists for this period."
        Dim r = _service.CreateRegularRun(2026, 10)
        Assert.False(r.Succeeded)
        Assert.Equal("A run already exists for this period.", r.Message)
    End Sub

    <Fact>
    Public Sub CreateRegularRun_BadMonth_IsRefused()
        Assert.False(_service.CreateRegularRun(2026, 13).Succeeded)
    End Sub

    <Fact>
    Public Sub Process_SavesLinesAndSetsProcessed()
        TwoEmployees()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Draft)
        Dim r = _service.Process(id)
        Assert.True(r.Succeeded)
        Assert.Equal(2, r.Value)
        Assert.Equal(PayrollRunStatus.Processed, _runs.StatusOf(id))
        Assert.Equal(2, _runs.Lines(id).Count)
        Assert.Equal("PayrollRun.Process", _audit.Entries(0).Action)
        Assert.DoesNotContain("14270", _audit.Entries(0).Details)
    End Sub

    <Fact>
    Public Sub Process_CanRunAgainWhenProcessed()
        TwoEmployees()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Processed)
        Assert.True(_service.Process(id).Succeeded)
    End Sub

    <Fact>
    Public Sub Process_RateFailure_StopsWithItsMessage()
        TwoEmployees()
        _rates.EpfFailure = "No EPF rate row is in force."
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Draft)
        Dim r = _service.Process(id)
        Assert.False(r.Succeeded)
        Assert.Equal("No EPF rate row is in force.", r.Message)
        Assert.Equal(PayrollRunStatus.Draft, _runs.StatusOf(id))
        Assert.False(_runs.Lines.ContainsKey(id))
    End Sub

    <Fact>
    Public Sub Process_OneEmployeeFails_NothingIsSaved()
        _runs.Inputs.Add(RunTestData.Facts(101, "E101", paidDays:=30D))
        _runs.Inputs.Add(RunTestData.Facts(102, "E102", withSalary:=False))
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Draft)
        Dim r = _service.Process(id)
        Assert.False(r.Succeeded)
        Assert.Contains("E102", r.Message)
        Assert.False(_runs.Lines.ContainsKey(id))
        Assert.Equal(PayrollRunStatus.Draft, _runs.StatusOf(id))
    End Sub

    <Fact>
    Public Sub Process_ManyFailures_ShowsFiveAndCountsTheRest()
        For i As Integer = 1 To 8
            _runs.Inputs.Add(RunTestData.Facts(100 + i, "E" & i.ToString(), withSalary:=False))
        Next
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Draft)
        Dim r = _service.Process(id)
        Assert.False(r.Succeeded)
        Assert.Contains("And 3 more.", r.Message)
        Assert.DoesNotContain("E6", r.Message)
    End Sub

    <Fact>
    Public Sub Process_NoEmployees_Fails()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Draft)
        Assert.False(_service.Process(id).Succeeded)
    End Sub

    <Fact>
    Public Sub Process_ApprovedRun_IsRefused()
        TwoEmployees()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Approved)
        Assert.False(_service.Process(id).Succeeded)
    End Sub

    <Fact>
    Public Sub Process_UnknownRun_IsRefused()
        Assert.False(_service.Process(999).Succeeded)
    End Sub

    <Fact>
    Public Sub Process_WithoutEdit_IsRefused()
        TwoEmployees()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Draft)
        Assert.False(_service.Process(id).Succeeded)
    End Sub

    <Fact>
    Public Sub PlanWithoutPayroll_RefusesEverything()
        _plan.PayrollEnabled = False
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Processed)
        Assert.False(_service.Process(id).Succeeded)
        Assert.False(_service.Approve(id).Succeeded)
        Assert.False(_service.Lock(id).Succeeded)
        Assert.False(_service.GetRun(id).Succeeded)
    End Sub

    <Fact>
    Public Sub Submit_PlanWithoutApproval_IsRefused()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Processed)
        Assert.False(_service.Submit(id).Succeeded)
    End Sub

    <Fact>
    Public Sub Submit_WithApproval_SubmitsAndAsksForApproval()
        _approvals.Required = True
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Processed, 120)
        Dim r = _service.Submit(id)
        Assert.True(r.Succeeded)
        Assert.Equal(PayrollRunStatus.Submitted, _runs.StatusOf(id))
        Assert.Equal("PayrollRun", _approvals.LastEntityName)
        Assert.Equal(id.ToString(), _approvals.LastEntityId)
        Assert.Contains("120 employees", _approvals.LastSummary)
        Assert.True(_approvals.LastSummary.Length <= 500)
    End Sub

    <Fact>
    Public Sub Submit_ApprovalRequestFails_RunGoesBackToDraft()
        _approvals.Required = True
        _approvals.SubmitFailure = "Approval service is down."
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Processed)
        Dim r = _service.Submit(id)
        Assert.False(r.Succeeded)
        Assert.Equal("Approval service is down.", r.Message)
        Assert.Equal(PayrollRunStatus.Draft, _runs.StatusOf(id))
    End Sub

    <Fact>
    Public Sub Submit_DraftRun_IsRefused()
        _approvals.Required = True
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Draft)
        Assert.False(_service.Submit(id).Succeeded)
    End Sub

    <Fact>
    Public Sub Approve_WithApproval_NeedsApprovePermission()
        _approvals.Required = True
        _approvals.Cleared = True
        _perms.Allowed.Remove(PermissionAction.Approve)
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Submitted)
        Assert.False(_service.Approve(id).Succeeded)
    End Sub

    <Fact>
    Public Sub Approve_WithApproval_NeedsApprovedRequest()
        _approvals.Required = True
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Submitted)
        Dim r = _service.Approve(id)
        Assert.False(r.Succeeded)
        Assert.Equal(PayrollRunStatus.Submitted, _runs.StatusOf(id))
    End Sub

    <Fact>
    Public Sub Approve_WithApproval_ClearedRequest_Approves()
        _approvals.Required = True
        _approvals.Cleared = True
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Submitted)
        Assert.True(_service.Approve(id).Succeeded)
        Assert.Equal(PayrollRunStatus.Approved, _runs.StatusOf(id))
        Assert.Equal("PayrollRun.Approve", _audit.Entries(0).Action)
    End Sub

    <Fact>
    Public Sub Approve_WithApproval_ProcessedRun_IsRefused()
        _approvals.Required = True
        _approvals.Cleared = True
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Processed)
        Assert.False(_service.Approve(id).Succeeded)
    End Sub

    <Fact>
    Public Sub Approve_SubmitterCannotApprove_ShowsDatabaseMessage()
        _approvals.Required = True
        _approvals.Cleared = True
        _runs.FailStatus = PayrollRunStatus.Approved
        _runs.FailStatusMessage = "The person who submitted the run cannot approve it."
        _runs.FailStatusNumber = 50329
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Submitted)
        Dim r = _service.Approve(id)
        Assert.False(r.Succeeded)
        Assert.Equal("The person who submitted the run cannot approve it.", r.Message)
        Assert.Equal(PayrollRunStatus.Submitted, _runs.StatusOf(id))
    End Sub

    <Fact>
    Public Sub Approve_TotalsChanged_CancelsApprovalAndReturnsToDraft()
        _approvals.Required = True
        _approvals.Cleared = True
        _runs.FailStatus = PayrollRunStatus.Approved
        _runs.FailStatusMessage = "The totals changed after the run was submitted."
        _runs.FailStatusNumber = 50315
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Submitted)
        Dim r = _service.Approve(id)
        Assert.False(r.Succeeded)
        Assert.Contains("back in Draft", r.Message)
        Assert.Equal(PayrollRunStatus.Draft, _runs.StatusOf(id))
    End Sub

    <Fact>
    Public Sub Approve_PlanWithoutApproval_ApprovesProcessedRun()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Processed)
        Assert.True(_service.Approve(id).Succeeded)
        Assert.Equal(PayrollRunStatus.Approved, _runs.StatusOf(id))
    End Sub

    <Fact>
    Public Sub ReturnToDraft_FromSubmitted()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Submitted)
        Assert.True(_service.ReturnToDraft(id, "  Wrong attendance  ").Succeeded)
        Assert.Equal(PayrollRunStatus.Draft, _runs.StatusOf(id))
        Assert.Equal("ReturnToDraft:Draft:Wrong attendance", _runs.Changes(0))
    End Sub

    <Fact>
    Public Sub ReturnToDraft_ApprovedRun_IsRefused()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Approved)
        Assert.False(_service.ReturnToDraft(id, Nothing).Succeeded)
    End Sub

    <Fact>
    Public Sub Lock_LocksAndAppliesRecoveriesOldestFirst()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Approved)
        _runs.Lines(id) = New List(Of RunLineResult) From {RunTestData.Line(101, 3000D), RunTestData.Line(102, 0D)}
        _runs.Installments.Add(New EmployeeInstallment(101, New OpenInstallment(1, 2, 202610, 1000D)))
        _runs.Installments.Add(New EmployeeInstallment(101, New OpenInstallment(1, 1, 202609, 2000D)))
        Dim r = _service.Lock(id)
        Assert.True(r.Succeeded)
        Assert.Equal(PayrollRunStatus.Locked, _runs.StatusOf(id))
        Assert.Equal(2, _runs.Recoveries.Count)
        Assert.Equal(1, _runs.Recoveries(0).InstallmentNo)
        Assert.Equal(2000D, _runs.Recoveries(0).Amount)
        Assert.Equal(2, _runs.Recoveries(1).InstallmentNo)
        Assert.Equal(1000D, _runs.Recoveries(1).Amount)
    End Sub

    <Fact>
    Public Sub Lock_NoLoans_SavesNoRecoveries()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Approved)
        _runs.Lines(id) = New List(Of RunLineResult) From {RunTestData.Line(101, 0D)}
        Assert.True(_service.Lock(id).Succeeded)
        Assert.Empty(_runs.Recoveries)
    End Sub

    <Fact>
    Public Sub Lock_ProcessedRun_IsRefused()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Processed)
        Assert.False(_service.Lock(id).Succeeded)
    End Sub

    <Fact>
    Public Sub Lock_RecoveriesFail_RunStaysLocked_AndRetryWorks()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Approved)
        _runs.Lines(id) = New List(Of RunLineResult) From {RunTestData.Line(101, 1000D)}
        _runs.Installments.Add(New EmployeeInstallment(101, New OpenInstallment(1, 1, 202610, 1000D)))
        _runs.FailRecoveryMessage = "Could not save."
        _runs.FailRecoveryNumber = 50327
        Dim first = _service.Lock(id)
        Assert.False(first.Succeeded)
        Assert.Contains("locked", first.Message)
        Assert.Equal(PayrollRunStatus.Locked, _runs.StatusOf(id))
        _runs.FailRecoveryMessage = Nothing
        Assert.True(_service.ApplyLoanRecoveries(id).Succeeded)
        Assert.Single(_runs.Recoveries)
    End Sub

    <Fact>
    Public Sub ApplyLoanRecoveries_AlreadyApplied_IsOk()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Locked)
        _runs.Lines(id) = New List(Of RunLineResult) From {RunTestData.Line(101, 1000D)}
        _runs.Installments.Add(New EmployeeInstallment(101, New OpenInstallment(1, 1, 202610, 1000D)))
        _runs.FailRecoveryMessage = "Recoveries are already applied for this run."
        _runs.FailRecoveryNumber = 50328
        Assert.True(_service.ApplyLoanRecoveries(id).Succeeded)
    End Sub

    <Fact>
    Public Sub ApplyLoanRecoveries_EmiMoreThanOutstanding_Fails()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Locked)
        _runs.Lines(id) = New List(Of RunLineResult) From {RunTestData.Line(101, 5000D)}
        _runs.Installments.Add(New EmployeeInstallment(101, New OpenInstallment(1, 1, 202610, 1000D)))
        Assert.False(_service.ApplyLoanRecoveries(id).Succeeded)
        Assert.Empty(_runs.Recoveries)
    End Sub

    <Fact>
    Public Sub ApplyLoanRecoveries_NotLocked_IsRefused()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Approved)
        Assert.False(_service.ApplyLoanRecoveries(id).Succeeded)
    End Sub

    <Fact>
    Public Sub Reopen_NeedsReasonOfTenCharacters()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Locked)
        Assert.False(_service.Reopen(id, "short").Succeeded)
        Assert.False(_service.Reopen(id, Nothing).Succeeded)
        Assert.Equal(PayrollRunStatus.Locked, _runs.StatusOf(id))
    End Sub

    <Fact>
    Public Sub Reopen_ReturnsToDraftAndReversesRecoveries()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Locked)
        Dim r = _service.Reopen(id, "  Wrong attendance for line 3  ")
        Assert.True(r.Succeeded)
        Assert.Equal(PayrollRunStatus.Draft, _runs.StatusOf(id))
        Assert.True(_runs.RecoveriesReversed)
        Assert.Equal("Reopen:Draft:Wrong attendance for line 3", _runs.Changes(0))
        Assert.Equal("PayrollRun.Reopen", _audit.Entries(0).Action)
    End Sub

    <Fact>
    Public Sub Reopen_WithoutReopenPermission_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Reopen)
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Locked)
        Assert.False(_service.Reopen(id, "A proper written reason").Succeeded)
        Assert.False(_runs.RecoveriesReversed)
    End Sub

    <Fact>
    Public Sub Reopen_ApprovedRun_IsRefused()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Approved)
        Assert.False(_service.Reopen(id, "A proper written reason").Succeeded)
    End Sub

    <Fact>
    Public Sub MarkPaid_ThenReopen_IsRefused()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Locked)
        Assert.True(_service.MarkPaid(id).Succeeded)
        Assert.Equal(PayrollRunStatus.Paid, _runs.StatusOf(id))
        Assert.False(_service.Reopen(id, "A proper written reason").Succeeded)
    End Sub

    <Fact>
    Public Sub MarkPaid_ApprovedRun_IsRefused()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Approved)
        Assert.False(_service.MarkPaid(id).Succeeded)
    End Sub

    <Fact>
    Public Sub Reads_NeedView_AndKnowMissingRun()
        Dim id As Integer = _runs.AddRun(PayrollRunStatus.Draft)
        Assert.True(_service.GetRun(id).Succeeded)
        Assert.True(_service.ListLines(id).Succeeded)
        Assert.True(_service.ListEvents(id).Succeeded)
        Assert.False(_service.GetRun(999).Succeeded)
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.GetRun(id).Succeeded)
    End Sub
End Class
