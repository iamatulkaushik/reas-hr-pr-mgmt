Imports System.Globalization
Imports Revolution.Core

''' <summary>Shared steps of the three master services: permission on Masters, validation messages, user id, audit.</summary>
Friend NotInheritable Class OrgMasterGate
    Private Const NotAllowed As String = "You are not allowed to do this."

    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext

    Friend Sub New(permissions As IPermissionService, audit As IAuditService, session As ISessionContext)
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        _permissions = permissions
        _audit = audit
        _session = session
    End Sub

    Friend Function CanView() As Boolean
        Return _permissions.IsAllowed(ModuleKey.Masters, PermissionAction.View)
    End Function

    ''' <summary>Create for a new row (id 0), Edit for an existing one.</summary>
    Friend Function CanSave(isNew As Boolean) As Boolean
        Return _permissions.IsAllowed(ModuleKey.Masters, If(isNew, PermissionAction.Create, PermissionAction.Edit))
    End Function

    Friend Shared Function Denied(Of T)() As OperationResult(Of T)
        Return OperationResult(Of T).Fail(NotAllowed)
    End Function

    Friend Shared Function Problems(found As IReadOnlyList(Of String)) As String
        Return String.Join(" ", found)
    End Function

    Friend Function Record(isNew As Boolean, entity As String, id As Integer, code As String) As OperationResult
        Dim action As String = If(isNew, "Create", "Update")
        Return _audit.Record(New AuditEntry(ModuleKey.Masters, action, entity, id.ToString(CultureInfo.InvariantCulture), $"Code {code}"))
    End Function
End Class
