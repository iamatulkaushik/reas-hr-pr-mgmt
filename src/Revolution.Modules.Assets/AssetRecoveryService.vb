Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' Recovery of a lost or damaged asset from the employee who held it (Phase 9 step 0601a, Platinum plan, Owner only). The default amount is the depreciated value today
''' (whole rupees, straight line over the type's useful life). The Owner may decide another amount, up to the purchase value, with a reason. Permissions on Assets:
''' View to look, Create to record a recovery, Edit to cancel an open one. The audit text holds no amount.
''' </summary>
Public NotInheritable Class AssetRecoveryService
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const NotInPlan As String = "Assets are not part of your plan."
    Private Const MinReasonLength As Integer = 3
    Private Const MaxReasonLength As Integer = 200

    Private ReadOnly _repository As IAssetRecoveryRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _today As Func(Of Date)

    Public Sub New(repository As IAssetRecoveryRepository, permissions As IPermissionService, entitlements As IEntitlementService, audit As IAuditService,
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

    ''' <summary>What a recovery of this asset would be today: who held it and the depreciated value (the default amount). Only a lost or damaged asset that was issued can be recovered.</summary>
    Public Function Quote(assetId As Integer) As OperationResult(Of AssetRecoveryQuote)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of AssetRecoveryQuote).Fail(allowed.Message)
        If assetId <= 0 Then Return OperationResult(Of AssetRecoveryQuote).Fail("Pick an asset.")
        Dim source As AssetRecoverySource
        Try
            source = _repository.Source(assetId)
        Catch ex As AssetsRuleException
            Return OperationResult(Of AssetRecoveryQuote).Fail(ex.Message)
        End Try
        If (source.Status <> AssetStatus.Lost AndAlso source.Status <> AssetStatus.Damaged) OrElse Not source.IssueId.HasValue Then
            Return OperationResult(Of AssetRecoveryQuote).Fail("Only a lost or damaged asset that was issued to an employee can be recovered.")
        End If
        Dim today As Date = _today().Date
        Return OperationResult(Of AssetRecoveryQuote).Ok(New AssetRecoveryQuote(source, AssetDepreciation.ValueOn(source.PurchaseValue, source.PurchaseDate, source.UsefulLifeMonths, today), today))
    End Function

    ''' <summary>Records a recovery. Amount Nothing means the depreciated value. Another amount needs a reason and cannot be above the purchase value. Returns the recovery id.</summary>
    Public Function Add(assetId As Integer, amount As Decimal?, reason As String) As OperationResult(Of Integer)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Create)
        If Not allowed.Succeeded Then Return OperationResult(Of Integer).Fail(allowed.Message)
        Dim quote As OperationResult(Of AssetRecoveryQuote) = Me.Quote(assetId)
        If Not quote.Succeeded Then Return OperationResult(Of Integer).Fail(quote.Message)
        Dim q As AssetRecoveryQuote = quote.Value
        Dim chosen As Decimal = If(amount.HasValue, Math.Round(amount.Value, 0, MidpointRounding.AwayFromZero), q.DepreciatedValue)
        If chosen <= 0D Then
            Return OperationResult(Of Integer).Fail(If(amount.HasValue, "The recovery amount must be more than 0 rupees.", "The asset is fully depreciated, so there is nothing to recover. Give an amount and a reason to recover something."))
        End If
        If chosen > q.Source.PurchaseValue Then Return OperationResult(Of Integer).Fail("The recovery cannot be more than the purchase value.")
        Dim cleanReason As String = If(reason, String.Empty).Trim()
        If cleanReason.Length > MaxReasonLength Then Return OperationResult(Of Integer).Fail($"The reason can have at most {MaxReasonLength} characters.")
        If chosen <> q.DepreciatedValue AndAlso cleanReason.Length < MinReasonLength Then
            Return OperationResult(Of Integer).Fail("Give a reason for an amount other than the depreciated value.")
        End If
        Dim id As Integer
        Try
            id = _repository.Add(New AssetRecoveryAdd With {.AssetId = assetId, .BasisDate = q.AsOf, .DepreciatedValue = q.DepreciatedValue, .Amount = chosen, .Reason = cleanReason}, CurrentUserId())
        Catch ex As AssetsRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = Log("AssetRecovery.Add", id, "Asset recovery recorded.")
        If Not logged.Succeeded Then Return OperationResult(Of Integer).Fail(logged.Message)
        Return OperationResult(Of Integer).Ok(id)
    End Function

    ''' <summary>Cancels an open recovery, with a reason. One a run holds, or the settlement took, cannot be cancelled.</summary>
    Public Function Cancel(recoveryId As Integer, reason As String) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return allowed
        If recoveryId <= 0 Then Return OperationResult.Fail("Pick a recovery.")
        Dim cleanReason As String = If(reason, String.Empty).Trim()
        If cleanReason.Length < MinReasonLength OrElse cleanReason.Length > MaxReasonLength Then Return OperationResult.Fail("Give a reason of 3 to 200 characters.")
        Try
            _repository.Cancel(recoveryId, cleanReason, CurrentUserId())
        Catch ex As AssetsRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return Log("AssetRecovery.Cancel", recoveryId, "Asset recovery cancelled.")
    End Function

    Public Function List(employeeId As Integer?, status As AssetRecoveryStatus?) As OperationResult(Of IReadOnlyList(Of AssetRecoveryItem))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of AssetRecoveryItem)).Fail(allowed.Message)
        Return OperationResult(Of IReadOnlyList(Of AssetRecoveryItem)).Ok(_repository.List(employeeId, status))
    End Function

    Private Function CheckAccess(action As PermissionAction) As OperationResult
        If Not _entitlements.IsEnabled(PlanFeature.AssetsAndExpenses) Then Return OperationResult.Fail(NotInPlan)
        If Not _permissions.IsAllowed(ModuleKey.Assets, action) Then Return OperationResult.Fail(NotAllowed)
        Return OperationResult.Ok()
    End Function

    Private Function Log(action As String, id As Integer, summary As String) As OperationResult
        Return _audit.Record(New AuditEntry(ModuleKey.Assets, action, "AssetRecovery", id.ToString(CultureInfo.InvariantCulture), summary))
    End Function

    Private Function CurrentUserId() As Integer?
        Dim user As UserSession = _session.Current
        Return If(user Is Nothing, CType(Nothing, Integer?), user.UserId)
    End Function
End Class
