Imports System.Globalization
Imports System.Text.RegularExpressions
Imports Revolution.Core

''' <summary>
''' Asset types, assets and hand-overs for the signed-in company (Phase 9, Platinum plan, PlanFeature.AssetsAndExpenses). Permissions on Assets: View to look, Create to add a type or an asset or to issue one,
''' Edit to change a type, take an asset back or mark it lost, damaged or written off. Only the Owner holds these rights (migration 0034). The audit text holds no value and no serial number.
''' </summary>
Public NotInheritable Class AssetService
    Public Const MaxLifeMonths As Integer = 600
    Public Const MaxValue As Decimal = 9999999.99D
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const NotInPlan As String = "Assets are not part of your plan."
    Private Const BadValues As String = "The values are not valid."

    Private Shared ReadOnly TagPattern As New Regex("^[A-Za-z0-9._/-]{1,30}$", RegexOptions.Compiled Or RegexOptions.CultureInvariant)

    Private ReadOnly _repository As IAssetRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _today As Func(Of Date)

    Public Sub New(repository As IAssetRepository, permissions As IPermissionService, entitlements As IEntitlementService, audit As IAuditService,
                   session As ISessionContext, today As Func(Of Date))
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        If today Is Nothing Then Throw New ArgumentNullException(NameOf(today))
        _repository = repository
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
        _session = session
        _today = today
    End Sub

    Public Function ListTypes(includeInactive As Boolean) As OperationResult(Of IReadOnlyList(Of AssetTypeItem))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of AssetTypeItem)).Fail(allowed.Message)
        Return OperationResult(Of IReadOnlyList(Of AssetTypeItem)).Ok(_repository.ListTypes(includeInactive))
    End Function

    ''' <summary>Adds an asset type (AssetTypeId Nothing, needs Create) or changes one (needs Edit). Returns its id.</summary>
    Public Function SaveType(assetType As AssetTypeWrite) As OperationResult(Of Integer)
        If assetType Is Nothing Then Return OperationResult(Of Integer).Fail("Give the asset type.")
        Dim allowed As OperationResult = CheckAccess(If(assetType.AssetTypeId.HasValue, PermissionAction.Edit, PermissionAction.Create))
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        Dim name As String = If(assetType.Name, String.Empty).Trim()
        If name.Length = 0 OrElse name.Length > 60 Then Return OperationResult(Of Integer).Fail("Give the asset type a name of 1 to 60 characters.")
        If assetType.UsefulLifeMonths < 1 OrElse assetType.UsefulLifeMonths > MaxLifeMonths Then
            Return OperationResult(Of Integer).Fail($"Useful life must be 1 to {MaxLifeMonths} months.")
        End If
        Dim clean As New AssetTypeWrite With {.AssetTypeId = assetType.AssetTypeId, .Name = name, .UsefulLifeMonths = assetType.UsefulLifeMonths, .IsActive = assetType.IsActive}
        Dim id As Integer
        Try
            id = _repository.SaveType(clean, CurrentUserId())
        Catch ex As AssetsRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = Log(If(assetType.AssetTypeId.HasValue, "AssetType.Change", "AssetType.Add"), "AssetType", id, "Asset type saved.")
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    ''' <summary>Assets, optionally of one status or held by one employee. HeldOnly lists only items that are out with an employee.</summary>
    Public Function ListAssets(status As AssetStatus?, employeeId As Integer?, heldOnly As Boolean) As OperationResult(Of IReadOnlyList(Of AssetItem))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of AssetItem)).Fail(allowed.Message)
        Return OperationResult(Of IReadOnlyList(Of AssetItem)).Ok(_repository.ListAssets(status, employeeId, heldOnly))
    End Function

    ''' <summary>Adds an asset to stock. The tag number is unique in the company. Returns its id.</summary>
    Public Function AddAsset(asset As AssetWrite) As OperationResult(Of Integer)
        If asset Is Nothing Then Return OperationResult(Of Integer).Fail("Give the asset.")
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Create)
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        Dim problem As String = ValidateAsset(asset)
        If problem.Length > 0 Then Return OperationResult(Of Integer).Fail(problem)
        Dim clean As New AssetWrite With {.AssetTypeId = asset.AssetTypeId, .TagNo = asset.TagNo.Trim(), .Description = asset.Description.Trim(),
                                          .SerialNo = If(asset.SerialNo, String.Empty).Trim(), .PurchaseDate = asset.PurchaseDate.Date,
                                          .PurchaseValue = Math.Round(asset.PurchaseValue, 2, MidpointRounding.AwayFromZero)}
        Dim id As Integer
        Try
            id = _repository.AddAsset(clean, CurrentUserId())
        Catch ex As AssetsRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = Log("Asset.Add", "Asset", id, "Asset added.")
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    ''' <summary>Hands an asset in stock to an employee. The day cannot be in the future.</summary>
    Public Function IssueAsset(issue As AssetIssueWrite) As OperationResult
        If issue Is Nothing Then Return OperationResult.Fail("Give the hand-over.")
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Create)
        If Not allowed.Succeeded Then Return allowed
        If issue.AssetId <= 0 Then Return OperationResult.Fail("Pick an asset.")
        If issue.EmployeeId <= 0 Then Return OperationResult.Fail("Pick an employee.")
        If issue.IssueDate.Date > _today().Date Then Return OperationResult.Fail("The issue date cannot be in the future.")
        If issue.ExpectedReturnDate.HasValue AndAlso issue.ExpectedReturnDate.Value.Date < issue.IssueDate.Date Then
            Return OperationResult.Fail("The expected return date is before the issue date.")
        End If
        If If(issue.Condition, String.Empty).Trim().Length > 100 Then Return OperationResult.Fail("The condition can have at most 100 characters.")
        Dim clean As New AssetIssueWrite With {.AssetId = issue.AssetId, .EmployeeId = issue.EmployeeId, .IssueDate = issue.IssueDate.Date,
                                               .Condition = If(issue.Condition, String.Empty).Trim(), .ExpectedReturnDate = issue.ExpectedReturnDate}
        Try
            _repository.IssueAsset(clean, CurrentUserId())
        Catch ex As AssetsRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return Log("Asset.Issue", "Asset", issue.AssetId, "Asset issued.")
    End Function

    ''' <summary>Takes an asset back from the employee holding it. It returns to stock, or to Damaged when ReturnStatus says so.</summary>
    Public Function ReturnAsset(back As AssetReturnWrite) As OperationResult
        If back Is Nothing Then Return OperationResult.Fail("Give the return.")
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return allowed
        If back.AssetId <= 0 Then Return OperationResult.Fail("Pick an asset.")
        If back.ReturnStatus <> AssetStatus.InStock AndAlso back.ReturnStatus <> AssetStatus.Damaged Then Return OperationResult.Fail("A returned asset is in stock or damaged.")
        If back.ReturnDate.Date > _today().Date Then Return OperationResult.Fail("The return date cannot be in the future.")
        If If(back.Condition, String.Empty).Trim().Length > 100 Then Return OperationResult.Fail("The condition can have at most 100 characters.")
        Dim clean As New AssetReturnWrite With {.AssetId = back.AssetId, .ReturnDate = back.ReturnDate.Date, .Condition = If(back.Condition, String.Empty).Trim(), .ReturnStatus = back.ReturnStatus}
        Try
            _repository.ReturnAsset(clean, CurrentUserId())
        Catch ex As AssetsRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return Log("Asset.Return", "Asset", back.AssetId, "Asset returned.")
    End Function

    ''' <summary>Marks an asset Lost, Damaged or WrittenOff. A written-off asset cannot change again.</summary>
    Public Function MarkAsset(assetId As Integer, status As AssetStatus) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return allowed
        If assetId <= 0 Then Return OperationResult.Fail("Pick an asset.")
        If status <> AssetStatus.Lost AndAlso status <> AssetStatus.Damaged AndAlso status <> AssetStatus.WrittenOff Then
            Return OperationResult.Fail("Mark an asset lost, damaged or written off.")
        End If
        Try
            _repository.SetStatus(assetId, status, CurrentUserId())
        Catch ex As AssetsRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return Log("Asset.Mark", "Asset", assetId, "Asset status changed.")
    End Function

    Public Function History(assetId As Integer) As OperationResult(Of IReadOnlyList(Of AssetIssueItem))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of AssetIssueItem)).Fail(allowed.Message)
        If assetId <= 0 Then Return OperationResult(Of IReadOnlyList(Of AssetIssueItem)).Fail("Pick an asset.")
        Return OperationResult(Of IReadOnlyList(Of AssetIssueItem)).Ok(_repository.History(assetId))
    End Function

    Private Function ValidateAsset(asset As AssetWrite) As String
        If asset.AssetTypeId <= 0 Then Return "Pick an asset type."
        Dim tag As String = If(asset.TagNo, String.Empty).Trim()
        If Not TagPattern.IsMatch(tag) Then Return "The tag number is 1 to 30 letters, digits or . _ / -."
        Dim description As String = If(asset.Description, String.Empty).Trim()
        If description.Length = 0 OrElse description.Length > 200 Then Return "Give a description of 1 to 200 characters."
        If If(asset.SerialNo, String.Empty).Trim().Length > 60 Then Return "The serial number can have at most 60 characters."
        If asset.PurchaseDate.Date > _today().Date Then Return "The purchase date cannot be in the future."
        If asset.PurchaseValue <= 0D OrElse asset.PurchaseValue > MaxValue Then Return "The purchase value must be more than 0 rupees."
        Return String.Empty
    End Function

    Private Function CheckAccess(action As PermissionAction) As OperationResult
        If Not _entitlements.IsEnabled(PlanFeature.AssetsAndExpenses) Then Return OperationResult.Fail(NotInPlan)
        If Not _permissions.IsAllowed(ModuleKey.Assets, action) Then Return OperationResult.Fail(NotAllowed)
        Return OperationResult.Ok()
    End Function

    Private Function Log(action As String, entity As String, id As Integer, summary As String) As OperationResult
        Return _audit.Record(New AuditEntry(ModuleKey.Assets, action, entity, id.ToString(CultureInfo.InvariantCulture), summary))
    End Function

    Private Function CurrentUserId() As Integer?
        Dim user As UserSession = _session.Current
        Return If(user Is Nothing, CType(Nothing, Integer?), user.UserId)
    End Function
End Class
