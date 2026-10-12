Imports System.Globalization
Imports Revolution.Core

''' <summary>EPF, ESI, PT, LWF and tax flags per employee. Permission, validation, storage, audit. Ids are not handled here.</summary>
Public NotInheritable Class EmployeeStatutoryService
    Private Const NotAllowed As String = "You are not allowed to do this."

    Private ReadOnly _repository As IStatutoryFlagsRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _today As Func(Of Date)

    Public Sub New(repository As IStatutoryFlagsRepository, permissions As IPermissionService, audit As IAuditService,
                   session As ISessionContext, today As Func(Of Date))
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        If today Is Nothing Then Throw New ArgumentNullException(NameOf(today))
        _repository = repository
        _permissions = permissions
        _audit = audit
        _session = session
        _today = today
    End Sub

    ''' <summary>The saved flags, or the database defaults when none are saved yet. Needs View on Employees.</summary>
    Public Function GetFlags(employeeId As Integer) As OperationResult(Of StatutoryFlags)
        If Not _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.View) Then
            Return OperationResult(Of StatutoryFlags).Fail(NotAllowed)
        End If
        If employeeId <= 0 Then
            Return OperationResult(Of StatutoryFlags).Fail("Pick an employee.")
        End If
        Return OperationResult(Of StatutoryFlags).Ok(If(_repository.GetFlags(employeeId), New StatutoryFlags()))
    End Function

    ''' <summary>Saves the flags. Needs Edit on Employees.</summary>
    Public Function SaveFlags(employeeId As Integer, flags As StatutoryFlags) As OperationResult
        If Not _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.Edit) Then
            Return OperationResult.Fail(NotAllowed)
        End If
        If employeeId <= 0 Then
            Return OperationResult.Fail("Pick an employee.")
        End If
        Dim problems = StatutoryFlagsValidator.Validate(flags, _today())
        If problems.Count > 0 Then
            Return OperationResult.Fail(String.Join(" ", problems))
        End If
        Try
            Dim user = _session.Current
            _repository.SaveFlags(employeeId, flags, If(user Is Nothing, CType(Nothing, Integer?), user.UserId))
        Catch ex As EmployeeRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return _audit.Record(New AuditEntry(ModuleKey.Employees, "SaveStatutory", "Employee",
                                            employeeId.ToString(CultureInfo.InvariantCulture),
                                            $"EPF {YesNo(flags.IsEpfMember)}, ESI {YesNo(flags.IsEsiMember)}, PT {YesNo(flags.PtApplicable)}, LWF {YesNo(flags.LwfApplicable)}"))
    End Function

    Private Shared Function YesNo(value As Boolean) As String
        Return If(value, "yes", "no")
    End Function
End Class
