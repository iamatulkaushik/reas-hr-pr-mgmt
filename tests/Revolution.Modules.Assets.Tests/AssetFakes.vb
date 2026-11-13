Imports Revolution.Core
Imports Revolution.Modules.Assets

Friend NotInheritable Class FakeAssetRepository
    Implements IAssetRepository

    Public ReadOnly Types As New List(Of AssetTypeItem)()
    Public ReadOnly Assets As New List(Of AssetItem)()
    Public ReadOnly SavedTypes As New List(Of AssetTypeWrite)()
    Public ReadOnly AddedAssets As New List(Of AssetWrite)()
    Public ReadOnly Issued As New List(Of AssetIssueWrite)()
    Public ReadOnly Returned As New List(Of AssetReturnWrite)()
    Public ReadOnly Marked As New List(Of Tuple(Of Integer, AssetStatus))()
    Public FailWith As String

    Private Sub MaybeFail()
        If FailWith IsNot Nothing Then Throw New AssetsRuleException(FailWith)
    End Sub

    Public Function ListTypes(includeInactive As Boolean) As IReadOnlyList(Of AssetTypeItem) Implements IAssetRepository.ListTypes
        Return Types.Where(Function(t) includeInactive OrElse t.IsActive).ToList()
    End Function

    Public Function SaveType(assetType As AssetTypeWrite, userId As Integer?) As Integer Implements IAssetRepository.SaveType
        MaybeFail()
        SavedTypes.Add(assetType)
        Return If(assetType.AssetTypeId, 100 + SavedTypes.Count)
    End Function

    Public Function ListAssets(status As AssetStatus?, employeeId As Integer?, heldOnly As Boolean) As IReadOnlyList(Of AssetItem) Implements IAssetRepository.ListAssets
        Return Assets.Where(Function(a) (Not status.HasValue OrElse a.Status = status.Value) AndAlso
                                        (Not employeeId.HasValue OrElse a.HolderEmployeeId = employeeId) AndAlso
                                        (Not heldOnly OrElse a.HolderEmployeeId.HasValue)).ToList()
    End Function

    Public Function AddAsset(asset As AssetWrite, userId As Integer?) As Integer Implements IAssetRepository.AddAsset
        MaybeFail()
        AddedAssets.Add(asset)
        Return 200 + AddedAssets.Count
    End Function

    Public Sub IssueAsset(issue As AssetIssueWrite, userId As Integer?) Implements IAssetRepository.IssueAsset
        MaybeFail()
        Issued.Add(issue)
    End Sub

    Public Sub ReturnAsset(back As AssetReturnWrite, userId As Integer?) Implements IAssetRepository.ReturnAsset
        MaybeFail()
        Returned.Add(back)
    End Sub

    Public Sub SetStatus(assetId As Integer, status As AssetStatus, userId As Integer?) Implements IAssetRepository.SetStatus
        MaybeFail()
        Marked.Add(Tuple.Create(assetId, status))
    End Sub

    Public Function History(assetId As Integer) As IReadOnlyList(Of AssetIssueItem) Implements IAssetRepository.History
        Return New List(Of AssetIssueItem) From {New AssetIssueItem(1, 5, "E5", "Asha", New Date(2026, 10, 1), "New", Nothing, Nothing, String.Empty)}
    End Function
End Class

Friend NotInheritable Class FakeAssetPermissions
    Implements IPermissionService

    Public ReadOnly Allowed As New HashSet(Of PermissionAction)()

    Public Function IsAllowed(moduleKey As ModuleKey, action As PermissionAction) As Boolean Implements IPermissionService.IsAllowed
        Return moduleKey = ModuleKey.Assets AndAlso Allowed.Contains(action)
    End Function
End Class

Friend NotInheritable Class FakeAssetEntitlements
    Implements IEntitlementService

    Public AssetsEnabled As Boolean = True

    Public ReadOnly Property CurrentPlan As PlanTier Implements IEntitlementService.CurrentPlan
        Get
            Return PlanTier.Platinum
        End Get
    End Property

    Public Function IsEnabled(feature As PlanFeature) As Boolean Implements IEntitlementService.IsEnabled
        Return feature <> PlanFeature.AssetsAndExpenses OrElse AssetsEnabled
    End Function
End Class

Friend NotInheritable Class FakeAssetAudit
    Implements IAuditService

    Public ReadOnly Entries As New List(Of AuditEntry)()

    Public Function Record(entry As AuditEntry) As OperationResult Implements IAuditService.Record
        Entries.Add(entry)
        Return OperationResult.Ok()
    End Function
End Class

Friend NotInheritable Class FakeAssetSession
    Implements ISessionContext

    Public ReadOnly Property Current As UserSession Implements ISessionContext.Current
        Get
            Return New UserSession(7, "owner1", UserRole.Owner, 1, PlanTier.Platinum)
        End Get
    End Property

    Public ReadOnly Property IsAuthenticated As Boolean Implements ISessionContext.IsAuthenticated
        Get
            Return True
        End Get
    End Property

    Public Sub SignIn(session As UserSession) Implements ISessionContext.SignIn
    End Sub

    Public Sub SignOut() Implements ISessionContext.SignOut
    End Sub
End Class
