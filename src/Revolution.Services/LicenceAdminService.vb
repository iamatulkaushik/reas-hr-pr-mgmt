Imports System.Globalization

''' <summary>
''' Developer-side licence handling (ADR 0018). A licence is only stored after it verifies against a trusted key for this installation
''' and has not already run past its grace period. The database repeats the Developer-only check.
''' </summary>
Public NotInheritable Class LicenceAdminService
    Implements ILicenceAdminService

    Private ReadOnly _repository As ILicenceRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _trustedKeys As IReadOnlyDictionary(Of String, String)
    Private ReadOnly _audit As IAuditService

    Private Const MaxReasonLength As Integer = 200

    Public Sub New(repository As ILicenceRepository, permissions As IPermissionService,
                   trustedKeys As IReadOnlyDictionary(Of String, String), audit As IAuditService)
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If trustedKeys Is Nothing Then Throw New ArgumentNullException(NameOf(trustedKeys))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        _repository = repository
        _permissions = permissions
        _trustedKeys = trustedKeys
        _audit = audit
    End Sub

    Public Function GetInstallationId() As Guid Implements ILicenceAdminService.GetInstallationId
        Return _repository.GetInstallation().Id
    End Function

    Public Function ListCurrent() As OperationResult(Of IReadOnlyList(Of LicenceOverview)) Implements ILicenceAdminService.ListCurrent
        If Not _permissions.IsAllowed(ModuleKey.Licence, PermissionAction.View) Then
            Return OperationResult(Of IReadOnlyList(Of LicenceOverview)).Fail("You do not have permission to see licences.")
        End If
        Dim stored = _repository.ListCurrent()
        If Not stored.Succeeded Then
            Return OperationResult(Of IReadOnlyList(Of LicenceOverview)).Fail(stored.Message)
        End If
        Dim installationId = _repository.GetInstallation().Id
        Dim today = _repository.GetServerToday()
        Dim overviews As New List(Of LicenceOverview)()
        For Each item In stored.Value
            overviews.Add(Describe(item, installationId, today))
        Next
        Return OperationResult(Of IReadOnlyList(Of LicenceOverview)).Ok(overviews)
    End Function

    Public Function Import(licenceText As String) As OperationResult Implements ILicenceAdminService.Import
        If Not _permissions.IsAllowed(ModuleKey.Licence, PermissionAction.Create) Then
            Return OperationResult.Fail("You do not have permission to import licences.")
        End If
        Dim text = If(licenceText, String.Empty).Trim()
        Dim check = LicenceVerifier.Verify(text, _trustedKeys, _repository.GetInstallation().Id)
        If Not check.Succeeded Then
            Return OperationResult.Fail(check.Message)
        End If
        If LicenceEvaluator.StateOn(check.Grant, _repository.GetServerToday()) = LicenceState.Expired Then
            Return OperationResult.Fail("This licence has already expired.")
        End If
        Dim saved = _repository.Import(check.Grant, text)
        If saved.Succeeded Then
            Dim details = $"{check.Grant.Associate}, {check.Grant.PlanCap}, until {check.Grant.ValidTo.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture)}"
            _audit.Record(New AuditEntry(ModuleKey.Licence, "LicenceImported", "Licence", check.Grant.LicenceId.ToString(), details))
        End If
        Return saved
    End Function

    Public Function Revoke(associateUserName As String, reason As String) As OperationResult Implements ILicenceAdminService.Revoke
        If Not _permissions.IsAllowed(ModuleKey.Licence, PermissionAction.Edit) Then
            Return OperationResult.Fail("You do not have permission to revoke licences.")
        End If
        Dim associate = If(associateUserName, String.Empty).Trim()
        Dim why = If(reason, String.Empty).Trim()
        If associate.Length = 0 Then
            Return OperationResult.Fail("Choose an Associate.")
        End If
        If why.Length = 0 Then
            Return OperationResult.Fail("Give a reason for revoking the licence.")
        End If
        If why.Length > MaxReasonLength Then
            Return OperationResult.Fail($"The reason can be at most {MaxReasonLength} characters.")
        End If
        Dim saved = _repository.Revoke(associate, why)
        If saved.Succeeded Then
            _audit.Record(New AuditEntry(ModuleKey.Licence, "LicenceRevoked", "Licence", associate, why))
        End If
        Return saved
    End Function

    Public Function Reinstate(associateUserName As String) As OperationResult Implements ILicenceAdminService.Reinstate
        If Not _permissions.IsAllowed(ModuleKey.Licence, PermissionAction.Edit) Then
            Return OperationResult.Fail("You do not have permission to reinstate licences.")
        End If
        Dim associate = If(associateUserName, String.Empty).Trim()
        If associate.Length = 0 Then
            Return OperationResult.Fail("Choose an Associate.")
        End If
        Dim saved = _repository.Reinstate(associate)
        If saved.Succeeded Then
            _audit.Record(New AuditEntry(ModuleKey.Licence, "LicenceReinstated", "Licence", associate))
        End If
        Return saved
    End Function

    Private Function Describe(stored As LicenceRecord, installationId As Guid, today As DateOnly) As LicenceOverview
        Dim check = LicenceVerifier.Verify(stored.LicenceText, _trustedKeys, installationId)
        If Not check.Succeeded Then
            Return Overview(stored, Nothing, check.Problem, check.Message)
        End If
        If Not String.Equals(check.Grant.Associate, stored.AssociateUserName, StringComparison.OrdinalIgnoreCase) Then
            Return Overview(stored, Nothing, LicenceProblem.WrongAssociate, "This licence was issued to a different Associate.")
        End If
        If stored.IsRevoked Then
            Return Overview(stored, LicenceState.Revoked, LicenceProblem.None, stored.RevokeReason)
        End If
        Return Overview(stored, LicenceEvaluator.StateOn(check.Grant, today), LicenceProblem.None, String.Empty)
    End Function

    Private Shared Function Overview(stored As LicenceRecord, state As LicenceState?, problem As LicenceProblem, message As String) As LicenceOverview
        Return New LicenceOverview(stored.AssociateUserName, stored.PlanCap, stored.ValidFrom, stored.ValidTo,
                                   stored.MaxCompanies, stored.MaxActiveUsers, state, problem, message)
    End Function
End Class
