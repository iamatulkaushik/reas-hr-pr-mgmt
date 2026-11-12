Imports Revolution.Core
Imports Revolution.Modules.Payroll
Imports Xunit

Friend NotInheritable Class FakeFnfAssetSource
    Implements IFnfAssetSource

    Public Facts As New FnfAssetFacts(0D, New List(Of String)())
    Public ReadOnly Reserved As New List(Of Tuple(Of Integer, Integer, Decimal))()
    Public ReadOnly Released As New List(Of Integer)()
    Public Loads As Integer
    Public ReserveFails As Boolean

    Public Function Load(employeeId As Integer, fnfId As Integer?) As FnfAssetFacts Implements IFnfAssetSource.Load
        Loads += 1
        Return Facts
    End Function

    Public Sub Reserve(fnfId As Integer, employeeId As Integer, total As Decimal) Implements IFnfAssetSource.Reserve
        If ReserveFails Then Throw New PayrollRuleException("The asset recoveries changed. Calculate the settlement again.", 53516, Nothing)
        Reserved.Add(Tuple.Create(fnfId, employeeId, total))
    End Sub

    Public Sub Release(fnfId As Integer) Implements IFnfAssetSource.Release
        Released.Add(fnfId)
    End Sub
End Class

''' <summary>Assets on the full and final settlement (Phase 9 step 0601b): the Asset recovery deduction, the block until assets are cleared, and holding the recoveries at approval.</summary>
Public Class FnfAssetTests
    Private Const AssetText As String = "Asset recovery"

    Private ReadOnly _repo As New FakeFnfRepository()
    Private ReadOnly _perms As New FakePayPermissions()
    Private ReadOnly _plan As New FakePayEntitlements()
    Private ReadOnly _assets As New FakeFnfAssetSource()
    Private ReadOnly _service As FnfService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Edit)
        _perms.Allowed.Add(PermissionAction.Approve)
        _perms.Allowed.Add(PermissionAction.Reopen)
        _service = New FnfService(_repo, _perms, _plan, New FakePayAudit(), New FakePaySession(), assets:=_assets)
    End Sub

    Private Shared Function Settlement(status As FnfStatus, ParamArray lines As FnfLine()) As FnfSettlementView
        Return New FnfSettlementView(42, 101, 9, New Date(2026, 10, 14), status, New FnfTotals(1000D, 0D), 8, Nothing, lines.ToList())
    End Function

    Private Shared Function AssetLine(amount As Decimal) As FnfLine
        Return New FnfLine(FnfComponentKind.OtherDeduction, AssetText, amount)
    End Function

    <Fact>
    Public Sub Approve_UnclearedAssets_AreRefused()
        _repo.Settlement = Settlement(FnfStatus.Calculated)
        _assets.Facts = New FnfAssetFacts(0D, New List(Of String) From {"LAP-001", "MOB-002"})
        Dim r = _service.Approve(9)
        Assert.False(r.Succeeded)
        Assert.Contains("LAP-001, MOB-002", r.Message)
        Assert.Empty(_repo.Changes)
        Assert.Empty(_assets.Reserved)
    End Sub

    Private Sub AssertRefusedWithRecoveryMismatch(r As OperationResult)
        Assert.False(r.Succeeded)
        Assert.Contains("changed", r.Message)
        Assert.Empty(_repo.Changes)
        Assert.Empty(_assets.Reserved)
    End Sub

    <Fact>
    Public Sub Approve_LinesWithoutTheAssetDeduction_AreRefused()
        _repo.Settlement = Settlement(FnfStatus.Calculated)
        _assets.Facts = New FnfAssetFacts(5000D, New List(Of String)())
        AssertRefusedWithRecoveryMismatch(_service.Approve(9))
    End Sub

    <Fact>
    Public Sub Approve_AssetLineLargerThanTheRecoveries_IsRefused()
        _repo.Settlement = Settlement(FnfStatus.Calculated, AssetLine(6000D))
        _assets.Facts = New FnfAssetFacts(5000D, New List(Of String)())
        AssertRefusedWithRecoveryMismatch(_service.Approve(9))
    End Sub

    <Fact>
    Public Sub Approve_MatchingLine_HoldsTheRecoveriesAndApproves()
        _repo.Settlement = Settlement(FnfStatus.Calculated, AssetLine(5000D))
        _assets.Facts = New FnfAssetFacts(5000D, New List(Of String)())
        Assert.True(_service.Approve(9).Succeeded)
        Dim held = _assets.Reserved.Single()
        Assert.Equal(42, held.Item1)
        Assert.Equal(101, held.Item2)
        Assert.Equal(5000D, held.Item3)
        Assert.Equal("42:Approved", _repo.Changes.Single())
    End Sub

    <Fact>
    Public Sub Approve_NothingToRecover_ApprovesWithoutHolding()
        _repo.Settlement = Settlement(FnfStatus.Calculated)
        Assert.True(_service.Approve(9).Succeeded)
        Assert.Empty(_assets.Reserved)
    End Sub

    <Fact>
    Public Sub Approve_PlanWithoutAssets_IgnoresAssets()
        _plan.Disabled.Add(PlanFeature.AssetsAndExpenses)
        _repo.Settlement = Settlement(FnfStatus.Calculated)
        _assets.Facts = New FnfAssetFacts(0D, New List(Of String) From {"LAP-001"})
        Assert.True(_service.Approve(9).Succeeded)
        Assert.Equal(0, _assets.Loads)
    End Sub

    <Fact>
    Public Sub Approve_HoldRefused_GivesTheMessageAndChangesNothing()
        _repo.Settlement = Settlement(FnfStatus.Calculated, AssetLine(5000D))
        _assets.Facts = New FnfAssetFacts(5000D, New List(Of String)())
        _assets.ReserveFails = True
        Dim r = _service.Approve(9)
        Assert.False(r.Succeeded)
        Assert.Contains("changed", r.Message)
        Assert.Empty(_repo.Changes)
    End Sub

    <Fact>
    Public Sub Approve_StatusChangeFails_PutsTheRecoveriesBack()
        _repo.Settlement = Settlement(FnfStatus.Calculated, AssetLine(5000D))
        _assets.Facts = New FnfAssetFacts(5000D, New List(Of String)())
        _repo.FailWith = "The settlement could not be saved."
        Assert.False(_service.Approve(9).Succeeded)
        Assert.Equal(42, _assets.Released.Single())
    End Sub

    <Fact>
    Public Sub Reopen_PutsTheRecoveriesBack()
        _repo.Settlement = Settlement(FnfStatus.Approved, AssetLine(5000D))
        Assert.True(_service.Reopen(9).Succeeded)
        Assert.Equal("42:Calculated", _repo.Changes.Single())
        Assert.Equal(42, _assets.Released.Single())
    End Sub

    <Fact>
    Public Sub MarkPaid_DoesNotTouchTheRecoveries()
        _repo.Settlement = Settlement(FnfStatus.Approved, AssetLine(5000D))
        Assert.True(_service.MarkPaid(9).Succeeded)
        Assert.Empty(_assets.Reserved)
        Assert.Empty(_assets.Released)
    End Sub
End Class

''' <summary>The asset deduction and the not-cleared note when the settlement is prepared. The employee is the ten-year employee of FnfPrepareTests.</summary>
Public Class FnfAssetPrepareTests
    Private ReadOnly _source As New FakeFnfFactsSource()
    Private ReadOnly _rates As New FakeRates()
    Private ReadOnly _plan As New FakePayEntitlements()
    Private ReadOnly _perms As New FakePayPermissions()
    Private ReadOnly _assets As New FakeFnfAssetSource()
    Private ReadOnly _with As FnfService
    Private ReadOnly _without As FnfService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _rates.GratuityRuleToReturn = New GratuityRule(New Date(2020, 4, 1), 5, 1, 15D, 26D, 6, 2000000D, True, Nothing, "test")
        _rates.BonusRuleToReturn = New BonusRule(New Date(2020, 4, 1), 21000D, 7000D, 8.33D, 20D, 30, True, Nothing, "test")
        _source.Facts = New FnfSourceFacts With {
            .EmployeeId = 101, .JoinDate = New Date(2015, 4, 1), .DateOfBirth = New Date(1985, 1, 1), .EmploymentTypeId = 1, .ReasonId = 1,
            .NoticeDate = New Date(2025, 3, 21), .LastWorkingDay = New Date(2025, 3, 31), .NoticePeriodDays = 30, .DailyWageDivisor = 26D,
            .MonthlyGross = 26000D, .MonthlyBasicPlusDa = 15000D, .LoanOutstanding = 5000D}
        Dim tds As New TdsProjectionService(New FakeTdsRates())
        _with = New FnfService(New FakeFnfRepository(), _perms, _plan, New FakePayAudit(), New FakePaySession(), _source, _rates, New FakeLeaveRepository(), New FakeRunTaxSource(), tds, _assets)
        _without = New FnfService(New FakeFnfRepository(), _perms, _plan, New FakePayAudit(), New FakePaySession(), _source, _rates, New FakeLeaveRepository(), New FakeRunTaxSource(), tds)
    End Sub

    <Fact>
    Public Sub Prepare_AddsTheAssetRecoveryAsOneDeduction()
        _assets.Facts = New FnfAssetFacts(8000D, New List(Of String)())
        Dim baseline = _without.Prepare(5, Nothing).Value
        Dim p = _with.Prepare(5, Nothing).Value
        Dim line = p.Lines.Single(Function(l) l.Kind = FnfComponentKind.OtherDeduction)
        Assert.Equal("Asset recovery", line.Description)
        Assert.Equal(8000D, line.Amount)
        Assert.Equal(baseline.Totals.Deductions + 8000D, p.Totals.Deductions)
        Assert.Equal(baseline.Totals.Earnings, p.Totals.Earnings)
    End Sub

    <Fact>
    Public Sub Prepare_NoRecovery_AddsNoLine()
        Assert.False(_with.Prepare(5, Nothing).Value.Lines.Any(Function(l) l.Kind = FnfComponentKind.OtherDeduction))
    End Sub

    <Fact>
    Public Sub Prepare_UnclearedAssets_AddANote_NotAnError()
        _assets.Facts = New FnfAssetFacts(0D, New List(Of String) From {"LAP-001"})
        Dim r = _with.Prepare(5, Nothing)
        Assert.True(r.Succeeded)
        Assert.Contains(r.Value.Notes, Function(n) n.Contains("LAP-001") AndAlso n.Contains("before approving"))
    End Sub

    <Fact>
    Public Sub Prepare_PlanWithoutAssets_LeavesAssetsOut()
        _plan.Disabled.Add(PlanFeature.AssetsAndExpenses)
        _assets.Facts = New FnfAssetFacts(8000D, New List(Of String) From {"LAP-001"})
        Dim p = _with.Prepare(5, Nothing).Value
        Assert.False(p.Lines.Any(Function(l) l.Kind = FnfComponentKind.OtherDeduction))
        Assert.Equal(0, _assets.Loads)
    End Sub
End Class
