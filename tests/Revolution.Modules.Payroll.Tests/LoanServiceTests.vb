Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class LoanServiceTests
    Private ReadOnly _repo As New FakeLoanRepository()
    Private ReadOnly _perms As New FakePayPermissions()
    Private ReadOnly _plan As New FakePayEntitlements()
    Private ReadOnly _audit As New FakePayAudit()
    Private ReadOnly _service As LoanService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Create)
        _perms.Allowed.Add(PermissionAction.Edit)
        _service = New LoanService(_repo, _perms, _plan, _audit, New FakePaySession())
    End Sub

    <Fact>
    Public Sub PlanWithoutLoans_RefusesEverything()
        _plan.Disabled.Add(PlanFeature.Loans)
        Assert.False(_service.ListTypes().Succeeded)
        Assert.False(_service.Sanction(101, 1, New Date(2026, 10, 25), 12000D, 12, 202611).Succeeded)
        Assert.False(_service.Cancel(5).Succeeded)
        Assert.Empty(_repo.Created)
    End Sub

    <Fact>
    Public Sub SaveType_New_NeedsCreate_TrimsAndAudits()
        Dim r = _service.SaveType(New LoanTypeItem(Nothing, "  Festival advance ", InterestMethod.Reducing, 9.5D, 24, True))
        Assert.True(r.Succeeded)
        Assert.Equal(77, r.Value)
        Assert.Equal("Festival advance", _repo.SavedTypes(0).Name)
        Assert.Equal("LoanType.Add", _audit.Entries(0).Action)
    End Sub

    <Fact>
    Public Sub SaveType_New_WithoutCreate_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Create)
        Assert.False(_service.SaveType(New LoanTypeItem(Nothing, "X", InterestMethod.Flat, 0D, 12, True)).Succeeded)
    End Sub

    <Fact>
    Public Sub SaveType_Change_NeedsEdit()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.SaveType(New LoanTypeItem(1, "Advance", InterestMethod.Flat, 12D, 12, True)).Succeeded)
    End Sub

    <Theory>
    <InlineData("", 12, 12, False)>
    <InlineData("Advance", 12, 12, True)>
    <InlineData("Advance", 101, 12, False)>
    <InlineData("Advance", -1, 12, False)>
    <InlineData("Advance", 0, 0, False)>
    <InlineData("Advance", 0, 361, False)>
    <InlineData("Advance", 0, 360, True)>
    Public Sub SaveType_Limits(name As String, pct As Decimal, maxInstallments As Integer, expected As Boolean)
        Assert.Equal(expected, _service.SaveType(New LoanTypeItem(Nothing, name, InterestMethod.Flat, pct, maxInstallments, True)).Succeeded)
    End Sub

    <Fact>
    Public Sub SaveType_DuplicateName_ShowsDatabaseMessage()
        _repo.FailWith = "A loan type with this name already exists."
        Dim r = _service.SaveType(New LoanTypeItem(Nothing, "Advance", InterestMethod.Flat, 0D, 12, True))
        Assert.False(r.Succeeded)
        Assert.Equal("A loan type with this name already exists.", r.Message)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub Sanction_CopiesTypeMethodAndRate_BuildsSchedule()
        Dim r = _service.Sanction(101, 1, New Date(2026, 10, 25), 12000D, 12, 202611)
        Assert.True(r.Succeeded)
        Assert.Equal(900, r.Value)
        Assert.Equal(InterestMethod.Flat, _repo.LastMethod)
        Assert.Equal(12D, _repo.LastRate)
        Assert.Equal(7, _repo.LastUserId)
        Assert.Equal(12, _repo.LastSchedule.Count)
        Assert.Equal(202611, _repo.LastSchedule(0).DueMonthKey)
        Assert.Equal(202710, _repo.LastSchedule(11).DueMonthKey)
        Assert.Equal(12000D, _repo.LastSchedule.Sum(Function(p) p.PrincipalPart))
        Assert.Equal(1120D, _repo.LastSchedule(0).Amount)
    End Sub

    <Fact>
    Public Sub Sanction_AuditHoldsNoAmount()
        _service.Sanction(101, 1, New Date(2026, 10, 25), 12000D, 12, 202611)
        Dim entry As AuditEntry = _audit.Entries(0)
        Assert.Equal("Loan.Sanction", entry.Action)
        Assert.Equal("900", entry.EntityId)
        Assert.DoesNotContain("12000", entry.Details)
        Assert.DoesNotContain("1120", entry.Details)
    End Sub

    <Fact>
    Public Sub Sanction_UnknownType_IsRefused()
        Assert.False(_service.Sanction(101, 99, New Date(2026, 10, 25), 12000D, 12, 202611).Succeeded)
        Assert.Empty(_repo.Created)
    End Sub

    <Fact>
    Public Sub Sanction_InactiveType_IsRefused()
        Assert.False(_service.Sanction(101, 2, New Date(2026, 10, 25), 3000D, 3, 202611).Succeeded)
    End Sub

    <Fact>
    Public Sub Sanction_MoreInstallmentsThanTypeAllows_IsRefused()
        Assert.False(_service.Sanction(101, 1, New Date(2026, 10, 25), 12000D, 13, 202611).Succeeded)
    End Sub

    <Fact>
    Public Sub Sanction_PrincipalWithPaise_IsRefused()
        Assert.False(_service.Sanction(101, 1, New Date(2026, 10, 25), 12000.5D, 12, 202611).Succeeded)
    End Sub

    <Theory>
    <InlineData(0)>
    <InlineData(-5)>
    Public Sub Sanction_PrincipalNotPositive_IsRefused(principal As Decimal)
        Assert.False(_service.Sanction(101, 1, New Date(2026, 10, 25), principal, 12, 202611).Succeeded)
    End Sub

    <Theory>
    <InlineData(202609)>
    <InlineData(202613)>
    <InlineData(202600)>
    <InlineData(0)>
    Public Sub Sanction_BadFirstDueMonth_IsRefused(key As Integer)
        Assert.False(_service.Sanction(101, 1, New Date(2026, 10, 25), 12000D, 12, key).Succeeded)
    End Sub

    <Fact>
    Public Sub Sanction_FirstDueInSanctionMonth_IsAllowed()
        Assert.True(_service.Sanction(101, 1, New Date(2026, 10, 25), 12000D, 12, 202610).Succeeded)
    End Sub

    <Fact>
    Public Sub Sanction_NoEmployee_IsRefused()
        Assert.False(_service.Sanction(0, 1, New Date(2026, 10, 25), 12000D, 12, 202611).Succeeded)
    End Sub

    <Fact>
    Public Sub Sanction_WithoutCreate_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Create)
        Assert.False(_service.Sanction(101, 1, New Date(2026, 10, 25), 12000D, 12, 202611).Succeeded)
    End Sub

    <Fact>
    Public Sub Sanction_DatabaseRule_ShowsMessage()
        _repo.FailWith = "The installments do not match the principal."
        Dim r = _service.Sanction(101, 1, New Date(2026, 10, 25), 12000D, 12, 202611)
        Assert.False(r.Succeeded)
        Assert.Equal("The installments do not match the principal.", r.Message)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub Sanction_AuditFailure_IsReported()
        _audit.Fail = True
        Assert.False(_service.Sanction(101, 1, New Date(2026, 10, 25), 12000D, 12, 202611).Succeeded)
    End Sub

    <Fact>
    Public Sub Preview_ReturnsScheduleWithoutSaving()
        Dim r = _service.PreviewSchedule(1, 12000D, 12, 202611)
        Assert.True(r.Succeeded)
        Assert.Equal(12, r.Value.Count)
        Assert.Equal(1120D, r.Value(0).Amount)
        Assert.Empty(_repo.Created)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub Preview_BadTerms_AreRefused()
        Assert.False(_service.PreviewSchedule(1, 12000D, 13, 202611).Succeeded)
        Assert.False(_service.PreviewSchedule(99, 12000D, 12, 202611).Succeeded)
        Assert.False(_service.PreviewSchedule(1, 12000D, 12, 0).Succeeded)
    End Sub

    <Fact>
    Public Sub Cancel_CallsRepositoryAndAudits()
        Assert.True(_service.Cancel(5).Succeeded)
        Assert.Equal(5, _repo.Cancelled.Single())
        Assert.Equal("Loan.Cancel", _audit.Entries(0).Action)
    End Sub

    <Fact>
    Public Sub Cancel_WithoutEdit_IsRefused()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.Cancel(5).Succeeded)
        Assert.Empty(_repo.Cancelled)
    End Sub

    <Fact>
    Public Sub Cancel_LoanWithRecoveries_ShowsDatabaseMessage()
        _repo.FailWith = "The loan has recoveries and cannot be cancelled."
        Dim r = _service.Cancel(5)
        Assert.False(r.Succeeded)
        Assert.Equal("The loan has recoveries and cannot be cancelled.", r.Message)
    End Sub

    <Fact>
    Public Sub Cancel_NoLoanId_IsRefused()
        Assert.False(_service.Cancel(0).Succeeded)
    End Sub

    <Fact>
    Public Sub ListLoans_NeedsViewAndEmployee()
        _repo.Loans.Add(New LoanSummary(1, 1, New Date(2026, 10, 25), 12000D, InterestMethod.Flat, 12D, 12, LoanStatus.Active, 13440D))
        Assert.Equal(1, _service.ListLoans(101).Value.Count)
        Assert.False(_service.ListLoans(0).Succeeded)
        _perms.Allowed.Remove(PermissionAction.View)
        Assert.False(_service.ListLoans(101).Succeeded)
    End Sub

    <Fact>
    Public Sub GetSchedule_ShowsOutstanding_AndUnknownLoanFails()
        Assert.False(_service.GetSchedule(1).Succeeded)
        _repo.Installments.Add(New LoanInstallmentView(1, 202611, 1000D, 120D, 1120D, 1000D))
        Dim r = _service.GetSchedule(1)
        Assert.True(r.Succeeded)
        Assert.Equal(120D, r.Value(0).Outstanding)
        Assert.False(_service.GetSchedule(0).Succeeded)
    End Sub
End Class
