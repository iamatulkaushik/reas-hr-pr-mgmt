Imports Revolution.Core
Imports Revolution.Modules.Assets
Imports Xunit

Friend NotInheritable Class FakeAssetRecoveryRepository
    Implements IAssetRecoveryRepository

    Public SourceRow As AssetRecoverySource
    Public ReadOnly Added As New List(Of AssetRecoveryAdd)()
    Public ReadOnly Cancelled As New List(Of Tuple(Of Integer, String))()
    Public ReadOnly Items As New List(Of AssetRecoveryItem)()
    Public FailWith As String

    Public Function Source(assetId As Integer) As AssetRecoverySource Implements IAssetRecoveryRepository.Source
        Return SourceRow
    End Function

    Public Function Add(recovery As AssetRecoveryAdd, userId As Integer?) As Integer Implements IAssetRecoveryRepository.Add
        If FailWith IsNot Nothing Then Throw New AssetsRuleException(FailWith)
        Added.Add(recovery)
        Return 300 + Added.Count
    End Function

    Public Sub Cancel(recoveryId As Integer, reason As String, userId As Integer?) Implements IAssetRecoveryRepository.Cancel
        If FailWith IsNot Nothing Then Throw New AssetsRuleException(FailWith)
        Cancelled.Add(Tuple.Create(recoveryId, reason))
    End Sub

    Public Function List(employeeId As Integer?, status As AssetRecoveryStatus?) As IReadOnlyList(Of AssetRecoveryItem) Implements IAssetRecoveryRepository.List
        Return Items.Where(Function(i) (Not employeeId.HasValue OrElse i.EmployeeId = employeeId) AndAlso (Not status.HasValue OrElse i.Status = status.Value)).ToList()
    End Function
End Class

Public Class AssetRecoveryServiceTests
    Private ReadOnly _repo As New FakeAssetRecoveryRepository()
    Private ReadOnly _perms As New FakeAssetPermissions()
    Private ReadOnly _plan As New FakeAssetEntitlements()
    Private ReadOnly _audit As New FakeAssetAudit()
    Private ReadOnly _service As AssetRecoveryService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Create)
        _perms.Allowed.Add(PermissionAction.Edit)
        _repo.SourceRow = Source(AssetStatus.Lost, 60000D, New Date(2026, 1, 15), 36, 77)
        _service = New AssetRecoveryService(_repo, _perms, _plan, _audit, New FakeAssetSession(), Function() New Date(2026, 11, 11))
    End Sub

    Private Shared Function Source(status As AssetStatus, value As Decimal, bought As Date, life As Integer, issueId As Integer?) As AssetRecoverySource
        Return New AssetRecoverySource(5, "LAP-001", "Laptop", bought, value, life, status, issueId, If(issueId.HasValue, 9, CType(Nothing, Integer?)), "E9", "Asha")
    End Function

    <Fact>
    Public Sub Quote_LostAsset_GivesTheDepreciatedValueToday()
        Dim r = _service.Quote(5)
        Assert.True(r.Succeeded)
        Assert.Equal(45000D, r.Value.DepreciatedValue)
        Assert.Equal(New Date(2026, 11, 11), r.Value.AsOf)
    End Sub

    <Theory>
    <InlineData(AssetStatus.InStock)>
    <InlineData(AssetStatus.Issued)>
    <InlineData(AssetStatus.WrittenOff)>
    Public Sub Quote_OnlyLostOrDamaged(status As AssetStatus)
        _repo.SourceRow = Source(status, 60000D, New Date(2026, 1, 15), 36, 77)
        Assert.False(_service.Quote(5).Succeeded)
    End Sub

    <Fact>
    Public Sub Quote_NeverIssued_IsRefused()
        _repo.SourceRow = Source(AssetStatus.Lost, 60000D, New Date(2026, 1, 15), 36, Nothing)
        Assert.False(_service.Quote(5).Succeeded)
    End Sub

    <Fact>
    Public Sub PlanWithoutAssets_RefusesEverything()
        _plan.AssetsEnabled = False
        Assert.False(_service.Quote(5).Succeeded)
        Assert.False(_service.Add(5, Nothing, "").Succeeded)
        Assert.False(_service.Cancel(1, "mistake").Succeeded)
        Assert.False(_service.List(Nothing, Nothing).Succeeded)
    End Sub

    <Fact>
    Public Sub Add_DefaultAmount_IsTheDepreciatedValue()
        Dim r = _service.Add(5, Nothing, Nothing)
        Assert.True(r.Succeeded)
        Dim saved = _repo.Added.Single()
        Assert.Equal(45000D, saved.Amount)
        Assert.Equal(45000D, saved.DepreciatedValue)
        Assert.Equal(New Date(2026, 11, 11), saved.BasisDate)
    End Sub

    <Fact>
    Public Sub Add_OtherAmount_NeedsAReason()
        Assert.False(_service.Add(5, 30000D, "").Succeeded)
        Assert.Empty(_repo.Added)
        Assert.True(_service.Add(5, 30000D, "  Owner waived a third  ").Succeeded)
        Assert.Equal("Owner waived a third", _repo.Added.Single().Reason)
        Assert.Equal(45000D, _repo.Added.Single().DepreciatedValue)
    End Sub

    <Fact>
    Public Sub Add_AmountIsRoundedToWholeRupees()
        Assert.True(_service.Add(5, 1000.5D, "Owner decided").Succeeded)
        Assert.Equal(1001D, _repo.Added.Single().Amount)
    End Sub

    <Theory>
    <InlineData(0)>
    <InlineData(-5)>
    <InlineData(60001)>
    Public Sub Add_AmountOutOfRange_IsRefused(amount As Integer)
        Assert.False(_service.Add(5, amount, "Owner decided").Succeeded)
        Assert.Empty(_repo.Added)
    End Sub

    <Fact>
    Public Sub Add_FullyDepreciated_NeedsAnExplicitAmount()
        _repo.SourceRow = Source(AssetStatus.Damaged, 60000D, New Date(2020, 1, 1), 36, 77)
        Assert.False(_service.Add(5, Nothing, Nothing).Succeeded)
        Assert.True(_service.Add(5, 5000D, "Owner decided").Succeeded)
        Assert.Equal(0D, _repo.Added.Single().DepreciatedValue)
    End Sub

    <Fact>
    Public Sub Add_NeedsCreate()
        _perms.Allowed.Remove(PermissionAction.Create)
        Assert.False(_service.Add(5, Nothing, Nothing).Succeeded)
        Assert.Empty(_repo.Added)
    End Sub

    <Fact>
    Public Sub Add_AlreadyRecovered_ComesBackAsAFailedResult()
        _repo.FailWith = "The asset already has a recovery."
        Dim r = _service.Add(5, Nothing, Nothing)
        Assert.False(r.Succeeded)
        Assert.Contains("already has a recovery", r.Message)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub Add_AuditHoldsNoAmount()
        Assert.True(_service.Add(5, 30000D, "Owner decided").Succeeded)
        Dim entry = _audit.Entries.Single()
        Assert.DoesNotContain("30000", entry.Details)
        Assert.DoesNotContain("45000", entry.Details)
    End Sub

    <Fact>
    Public Sub Cancel_NeedsEdit_AndAReason()
        Assert.False(_service.Cancel(1, "").Succeeded)
        Assert.False(_service.Cancel(1, "ab").Succeeded)
        Assert.True(_service.Cancel(1, "Item found").Succeeded)
        Assert.Equal("Item found", _repo.Cancelled.Single().Item2)
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.Cancel(1, "Item found").Succeeded)
    End Sub

    <Fact>
    Public Sub Cancel_NotOpen_ComesBackAsAFailedResult()
        _repo.FailWith = "The recovery is not open, so it cannot be cancelled."
        Dim r = _service.Cancel(1, "Item found")
        Assert.False(r.Succeeded)
        Assert.Contains("not open", r.Message)
    End Sub

    <Fact>
    Public Sub List_FiltersByEmployeeAndStatus()
        _repo.Items.Add(New AssetRecoveryItem(1, 5, "LAP-1", "Laptop", 9, "E9", "Asha", New Date(2026, 11, 1), 45000D, 45000D, String.Empty, AssetRecoveryStatus.Open, Nothing))
        _repo.Items.Add(New AssetRecoveryItem(2, 6, "MOB-1", "Mobile", 10, "E10", "Ravi", New Date(2026, 11, 1), 8000D, 5000D, "Owner decided", AssetRecoveryStatus.Recovered, 12))
        Assert.Equal(2, _service.List(Nothing, Nothing).Value.Count)
        Assert.Equal(1, _service.List(9, Nothing).Value.Count)
        Assert.Equal(1, _service.List(Nothing, AssetRecoveryStatus.Recovered).Value.Count)
    End Sub
End Class
