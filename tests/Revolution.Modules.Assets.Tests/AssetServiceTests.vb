Imports Revolution.Core
Imports Revolution.Modules.Assets
Imports Xunit

Public Class AssetServiceTests
    Private ReadOnly _repo As New FakeAssetRepository()
    Private ReadOnly _perms As New FakeAssetPermissions()
    Private ReadOnly _plan As New FakeAssetEntitlements()
    Private ReadOnly _audit As New FakeAssetAudit()
    Private ReadOnly _service As AssetService

    Public Sub New()
        _perms.Allowed.Add(PermissionAction.View)
        _perms.Allowed.Add(PermissionAction.Create)
        _perms.Allowed.Add(PermissionAction.Edit)
        _service = New AssetService(_repo, _perms, _plan, _audit, New FakeAssetSession(), Function() New Date(2026, 11, 11))
    End Sub

    Private Shared Function NewAsset() As AssetWrite
        Return New AssetWrite With {.AssetTypeId = 2, .TagNo = "LAP-001", .Description = "Dell laptop", .SerialNo = "SN-SECRET-9", .PurchaseDate = New Date(2026, 4, 1), .PurchaseValue = 54321.5D}
    End Function

    <Fact>
    Public Sub PlanWithoutAssets_RefusesEverything()
        _plan.AssetsEnabled = False
        Assert.False(_service.ListTypes(False).Succeeded)
        Assert.False(_service.ListAssets(Nothing, Nothing, False).Succeeded)
        Assert.False(_service.AddAsset(NewAsset()).Succeeded)
        Assert.Empty(_repo.AddedAssets)
    End Sub

    <Fact>
    Public Sub WithoutAnyRight_RefusesEverything()
        _perms.Allowed.Clear()
        Assert.False(_service.ListAssets(Nothing, Nothing, False).Succeeded)
        Assert.False(_service.AddAsset(NewAsset()).Succeeded)
        Assert.False(_service.SaveType(New AssetTypeWrite With {.Name = "Laptop", .UsefulLifeMonths = 36}).Succeeded)
        Assert.False(_service.MarkAsset(1, AssetStatus.Lost).Succeeded)
    End Sub

    <Fact>
    Public Sub SaveType_Add_NeedsCreate_AndTrimsTheName()
        Dim r = _service.SaveType(New AssetTypeWrite With {.Name = "  Laptop  ", .UsefulLifeMonths = 36})
        Assert.True(r.Succeeded)
        Assert.Equal("Laptop", _repo.SavedTypes.Single().Name)
        Assert.Single(_audit.Entries)
    End Sub

    <Fact>
    Public Sub SaveType_Change_NeedsEdit()
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.SaveType(New AssetTypeWrite With {.AssetTypeId = 3, .Name = "Laptop", .UsefulLifeMonths = 36}).Succeeded)
    End Sub

    <Theory>
    <InlineData("", 36)>
    <InlineData("Laptop", 0)>
    <InlineData("Laptop", 601)>
    Public Sub SaveType_BadValues_AreRefused(name As String, life As Integer)
        Assert.False(_service.SaveType(New AssetTypeWrite With {.Name = name, .UsefulLifeMonths = life}).Succeeded)
        Assert.Empty(_repo.SavedTypes)
    End Sub

    <Fact>
    Public Sub SaveType_DatabaseRule_ComesBackAsAFailedResult()
        _repo.FailWith = "The asset type name is already used."
        Dim r = _service.SaveType(New AssetTypeWrite With {.Name = "Laptop", .UsefulLifeMonths = 36})
        Assert.False(r.Succeeded)
        Assert.Contains("already used", r.Message)
    End Sub

    <Fact>
    Public Sub AddAsset_Valid_IsSavedTrimmedAndRounded()
        Dim item = NewAsset()
        item.TagNo = " LAP-001 "
        item.PurchaseValue = 100.005D
        Assert.True(_service.AddAsset(item).Succeeded)
        Assert.Equal("LAP-001", _repo.AddedAssets.Single().TagNo)
        Assert.Equal(100.01D, _repo.AddedAssets.Single().PurchaseValue)
    End Sub

    <Fact>
    Public Sub AddAsset_AuditHoldsNoValueAndNoSerial()
        Assert.True(_service.AddAsset(NewAsset()).Succeeded)
        Dim entry = _audit.Entries.Single()
        Assert.DoesNotContain("SN-SECRET-9", entry.Details)
        Assert.DoesNotContain("54321", entry.Details)
    End Sub

    <Theory>
    <InlineData(0, "LAP-001", "Dell", 100)>
    <InlineData(2, "bad tag!", "Dell", 100)>
    <InlineData(2, "LAP-001", "", 100)>
    <InlineData(2, "LAP-001", "Dell", 0)>
    <InlineData(2, "LAP-001", "Dell", 10000000)>
    Public Sub AddAsset_BadValues_AreRefused(typeId As Integer, tag As String, description As String, value As Integer)
        Dim item = NewAsset()
        item.AssetTypeId = typeId
        item.TagNo = tag
        item.Description = description
        item.PurchaseValue = value
        Assert.False(_service.AddAsset(item).Succeeded)
        Assert.Empty(_repo.AddedAssets)
    End Sub

    <Fact>
    Public Sub AddAsset_FutureDate_IsRefused()
        Dim item = NewAsset()
        item.PurchaseDate = New Date(2026, 11, 12)
        Assert.False(_service.AddAsset(item).Succeeded)
    End Sub

    <Fact>
    Public Sub AddAsset_TagUsed_ComesBackAsAFailedResult()
        _repo.FailWith = "The tag number is already used."
        Dim r = _service.AddAsset(NewAsset())
        Assert.False(r.Succeeded)
        Assert.Contains("tag number", r.Message)
        Assert.Empty(_audit.Entries)
    End Sub

    <Fact>
    Public Sub IssueAsset_Valid_IsSaved()
        Dim r = _service.IssueAsset(New AssetIssueWrite With {.AssetId = 5, .EmployeeId = 9, .IssueDate = New Date(2026, 11, 10), .Condition = " New ", .ExpectedReturnDate = New Date(2027, 11, 10)})
        Assert.True(r.Succeeded)
        Assert.Equal("New", _repo.Issued.Single().Condition)
    End Sub

    <Fact>
    Public Sub IssueAsset_FutureDay_IsRefused()
        Assert.False(_service.IssueAsset(New AssetIssueWrite With {.AssetId = 5, .EmployeeId = 9, .IssueDate = New Date(2026, 11, 12)}).Succeeded)
    End Sub

    <Fact>
    Public Sub IssueAsset_ReturnBeforeIssue_IsRefused()
        Assert.False(_service.IssueAsset(New AssetIssueWrite With {.AssetId = 5, .EmployeeId = 9, .IssueDate = New Date(2026, 11, 10), .ExpectedReturnDate = New Date(2026, 11, 9)}).Succeeded)
    End Sub

    <Fact>
    Public Sub IssueAsset_NoEmployee_IsRefused()
        Assert.False(_service.IssueAsset(New AssetIssueWrite With {.AssetId = 5, .EmployeeId = 0, .IssueDate = New Date(2026, 11, 10)}).Succeeded)
    End Sub

    <Fact>
    Public Sub IssueAsset_NotInStock_ComesBackAsAFailedResult()
        _repo.FailWith = "The asset is not in stock."
        Dim r = _service.IssueAsset(New AssetIssueWrite With {.AssetId = 5, .EmployeeId = 9, .IssueDate = New Date(2026, 11, 10)})
        Assert.False(r.Succeeded)
        Assert.Contains("not in stock", r.Message)
    End Sub

    <Fact>
    Public Sub ReturnAsset_DefaultsToInStock_AndNeedsEdit()
        Assert.True(_service.ReturnAsset(New AssetReturnWrite With {.AssetId = 5, .ReturnDate = New Date(2026, 11, 10)}).Succeeded)
        Assert.Equal(AssetStatus.InStock, _repo.Returned.Single().ReturnStatus)
        _perms.Allowed.Remove(PermissionAction.Edit)
        Assert.False(_service.ReturnAsset(New AssetReturnWrite With {.AssetId = 5, .ReturnDate = New Date(2026, 11, 10)}).Succeeded)
    End Sub

    <Theory>
    <InlineData(AssetStatus.Lost)>
    <InlineData(AssetStatus.Issued)>
    <InlineData(AssetStatus.WrittenOff)>
    Public Sub ReturnAsset_OnlyInStockOrDamaged(status As AssetStatus)
        Assert.False(_service.ReturnAsset(New AssetReturnWrite With {.AssetId = 5, .ReturnDate = New Date(2026, 11, 10), .ReturnStatus = status}).Succeeded)
    End Sub

    <Fact>
    Public Sub ReturnAsset_FutureDay_IsRefused()
        Assert.False(_service.ReturnAsset(New AssetReturnWrite With {.AssetId = 5, .ReturnDate = New Date(2026, 11, 12)}).Succeeded)
    End Sub

    <Theory>
    <InlineData(AssetStatus.Lost, True)>
    <InlineData(AssetStatus.Damaged, True)>
    <InlineData(AssetStatus.WrittenOff, True)>
    <InlineData(AssetStatus.InStock, False)>
    <InlineData(AssetStatus.Issued, False)>
    Public Sub MarkAsset_OnlyLostDamagedOrWrittenOff(status As AssetStatus, allowed As Boolean)
        Assert.Equal(allowed, _service.MarkAsset(5, status).Succeeded)
    End Sub

    <Fact>
    Public Sub History_NeedsAnAsset_AndReturnsRows()
        Assert.False(_service.History(0).Succeeded)
        Assert.Single(_service.History(5).Value)
    End Sub

    <Fact>
    Public Sub ListAssets_FiltersByHolder()
        _repo.Assets.Add(New AssetItem(1, 2, "Laptop", "LAP-1", "Dell", String.Empty, New Date(2026, 4, 1), 50000D, AssetStatus.Issued, 9, "E9", "Asha", New Date(2026, 5, 1), Nothing))
        _repo.Assets.Add(New AssetItem(2, 2, "Laptop", "LAP-2", "HP", String.Empty, New Date(2026, 4, 1), 40000D, AssetStatus.InStock, Nothing, String.Empty, String.Empty, Nothing, Nothing))
        Assert.Equal(1, _service.ListAssets(Nothing, 9, False).Value.Count)
        Assert.Equal(1, _service.ListAssets(Nothing, Nothing, True).Value.Count)
        Assert.Equal(2, _service.ListAssets(Nothing, Nothing, False).Value.Count)
    End Sub
End Class
