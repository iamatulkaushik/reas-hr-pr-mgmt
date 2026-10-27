Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Public Class FnfServiceTests
    Private ReadOnly _repo As New FakeFnfRepository()
    Private ReadOnly _perms As New FakePayPermissions()
    Private ReadOnly _plan As New FakePayEntitlements()
    Private ReadOnly _audit As New FakePayAudit()
    Private ReadOnly _service As FnfService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Edit)
        _perms.Allowed.Add(PermissionAction.Approve)
        _perms.Allowed.Add(PermissionAction.Reopen)
        _service = New FnfService(_repo, _perms, _plan, _audit, New FakePaySession())
    End Sub

    Private Shared Function OneLine() As IReadOnlyList(Of FnfLine)
        Return New List(Of FnfLine) From {New FnfLine(FnfComponentKind.LastSalary, "Salary", 20000D)}
    End Function

    <Fact>
    Public Sub PlanWithoutFullAndFinal_RefusesEverything()
        _plan.Disabled.Add(PlanFeature.FullAndFinal)
        Assert.False(_service.GetSettlement(9).Succeeded)
        Assert.False(_service.SaveLines(9, OneLine()).Succeeded)
        Assert.False(_service.Approve(9).Succeeded)
        Assert.Empty(_repo.Saved)
    End Sub

    <Fact>
    Public Sub SaveLines_NewSettlement_SavesAndAudits()
        Dim r = _service.SaveLines(9, OneLine())
        Assert.True(r.Succeeded)
        Assert.Equal(42, r.Value)
        Assert.Single(_repo.Saved)
        Assert.Equal("Fnf.Calculate", _audit.Entries.Single().Action)
    End Sub

    <Fact>
    Public Sub SaveLines_NeedsEdit_AndLines()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.SaveLines(9, OneLine()).Succeeded)
        _perms.Allowed.Add(PermissionAction.Edit)
        Assert.False(_service.SaveLines(9, New List(Of FnfLine)()).Succeeded)
        Assert.False(_service.SaveLines(0, OneLine()).Succeeded)
    End Sub

    <Fact>
    Public Sub SaveLines_ApprovedSettlement_Refused()
        _repo.Settlement = FakeFnfRepository.Make(FnfStatus.Approved, 8)
        Assert.False(_service.SaveLines(9, OneLine()).Succeeded)
        Assert.Empty(_repo.Saved)
    End Sub

    <Fact>
    Public Sub Approve_ByTheMaker_Refused()
        _repo.Settlement = FakeFnfRepository.Make(FnfStatus.Calculated, 7)
        Dim r = _service.Approve(9)
        Assert.False(r.Succeeded)
        Assert.Empty(_repo.Changes)
    End Sub

    <Fact>
    Public Sub Approve_ByAnotherUser_MovesAndAudits()
        _repo.Settlement = FakeFnfRepository.Make(FnfStatus.Calculated, 8)
        Assert.True(_service.Approve(9).Succeeded)
        Assert.Equal("42:Approved", _repo.Changes.Single())
        Assert.Equal("Fnf.Approve", _audit.Entries.Single().Action)
    End Sub

    <Fact>
    Public Sub Approve_NeedsApprovePermission()
        _perms.Allowed.Remove(PermissionAction.Approve)
        _repo.Settlement = FakeFnfRepository.Make(FnfStatus.Calculated, 8)
        Assert.False(_service.Approve(9).Succeeded)
    End Sub

    <Fact>
    Public Sub Approve_NoSettlement_Refused()
        Assert.False(_service.Approve(9).Succeeded)
    End Sub

    <Fact>
    Public Sub MarkPaid_OnlyFromApproved()
        _repo.Settlement = FakeFnfRepository.Make(FnfStatus.Calculated, 8)
        Assert.False(_service.MarkPaid(9).Succeeded)
        _repo.Settlement = FakeFnfRepository.Make(FnfStatus.Approved, 8)
        Assert.True(_service.MarkPaid(9).Succeeded)
        Assert.Equal("42:Paid", _repo.Changes.Single())
    End Sub

    <Fact>
    Public Sub Reopen_NeedsReopenPermission_AndApproved()
        _repo.Settlement = FakeFnfRepository.Make(FnfStatus.Approved, 8)
        _perms.Allowed.Remove(PermissionAction.Reopen)
        Assert.False(_service.Reopen(9).Succeeded)
        _perms.Allowed.Add(PermissionAction.Reopen)
        Assert.True(_service.Reopen(9).Succeeded)
        Assert.Equal("42:Calculated", _repo.Changes.Single())
    End Sub

    <Fact>
    Public Sub PaidSettlement_CannotMove()
        _repo.Settlement = FakeFnfRepository.Make(FnfStatus.Paid, 8)
        Assert.False(_service.Reopen(9).Succeeded)
        Assert.False(_service.Approve(9).Succeeded)
    End Sub

    <Fact>
    Public Sub DatabaseRuleError_BecomesFailure()
        _repo.Settlement = FakeFnfRepository.Make(FnfStatus.Calculated, 8)
        _repo.FailWith = "That move is not allowed."
        Dim r = _service.Approve(9)
        Assert.False(r.Succeeded)
        Assert.Equal("That move is not allowed.", r.Message)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub Preview_TotalsTheLines()
        Dim r = _service.Preview(New List(Of FnfLine) From {New FnfLine(FnfComponentKind.LastSalary, "Salary", 5000D), New FnfLine(FnfComponentKind.LoanClosure, "Loan", 2000D)})
        Assert.Equal(3000D, r.Value.Net)
    End Sub
End Class
