Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' Issue, list and retire the keys of the company's punch agents (special build, ADR 0021). Needs the Biometric plan feature; View to list, Create to issue, Edit to retire. The database also checks that
''' the user is the Owner or the Associate of the company. The key comes back once, in the result, and is never audited or kept. A lost key is retired and a new one issued.
''' </summary>
Public NotInheritable Class AgentKeyService
    Public Const MaxLabelLength As Integer = 60
    Private Const NotAllowed As String = "You are not allowed to do this."

    Private ReadOnly _repository As IAgentKeyRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _entitlements As IEntitlementService
    Private ReadOnly _audit As IAuditService

    Public Sub New(repository As IAgentKeyRepository, permissions As IPermissionService, entitlements As IEntitlementService, audit As IAuditService)
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        _repository = repository
        _permissions = permissions
        _entitlements = entitlements
        _audit = audit
    End Sub

    Public Function List() As OperationResult(Of IReadOnlyList(Of AgentKeyItem))
        Dim allowed As OperationResult = CheckAccess(PermissionAction.View)
        If Not allowed.Succeeded Then Return OperationResult(Of IReadOnlyList(Of AgentKeyItem)).Fail(allowed.Message)
        Try
            Return OperationResult(Of IReadOnlyList(Of AgentKeyItem)).Ok(_repository.List())
        Catch ex As BiometricRuleException
            Return OperationResult(Of IReadOnlyList(Of AgentKeyItem)).Fail(ex.Message)
        End Try
    End Function

    ''' <summary>Issues a key for an agent with a label (for example the computer name, 1 to 60 characters). Returns it once.</summary>
    Public Function Issue(label As String) As OperationResult(Of IssuedAgentKey)
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Create)
        If Not allowed.Succeeded Then Return OperationResult(Of IssuedAgentKey).Fail(allowed.Message)
        Dim text As String = If(label, String.Empty).Trim()
        If text.Length = 0 OrElse text.Length > MaxLabelLength Then Return OperationResult(Of IssuedAgentKey).Fail($"Give the agent a label of 1 to {MaxLabelLength.ToString(CultureInfo.InvariantCulture)} characters.")
        Dim issued As IssuedAgentKey
        Try
            issued = _repository.Issue(text)
        Catch ex As BiometricRuleException
            Return OperationResult(Of IssuedAgentKey).Fail(ex.Message)
        End Try
        Dim logged As OperationResult = _audit.Record(New AuditEntry(ModuleKey.Biometric, "AgentKey.Issue", "AgentRegistration", issued.RegistrationId.ToString(CultureInfo.InvariantCulture), "Agent key issued."))
        If Not logged.Succeeded Then Return OperationResult(Of IssuedAgentKey).Fail(logged.Message)
        Return OperationResult(Of IssuedAgentKey).Ok(issued)
    End Function

    Public Function Retire(registrationId As Integer) As OperationResult
        Dim allowed As OperationResult = CheckAccess(PermissionAction.Edit)
        If Not allowed.Succeeded Then Return allowed
        If registrationId <= 0 Then Return OperationResult.Fail("Pick an agent.")
        Try
            _repository.Retire(registrationId)
        Catch ex As BiometricRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Biometric, "AgentKey.Retire", "AgentRegistration", registrationId.ToString(CultureInfo.InvariantCulture), "Agent key retired."))
    End Function

    Private Function CheckAccess(action As PermissionAction) As OperationResult
        If Not _entitlements.IsEnabled(PlanFeature.Biometric) Then Return OperationResult.Fail("Biometric attendance is not part of your plan.")
        If Not _permissions.IsAllowed(ModuleKey.Biometric, action) Then Return OperationResult.Fail(NotAllowed)
        Return OperationResult.Ok()
    End Function
End Class
