Imports System.Globalization
Imports Revolution.Core

''' <summary>
''' Family members and nominations of an employee. Nominations are not mandatory. For each scheme the shares may total
''' up to 100 percent; a total below 100 is allowed. Needs View to read and Edit to change on Employees.
''' </summary>
Public NotInheritable Class EmployeeFamilyService
    Private Const NotAllowed As String = "You are not allowed to do this."
    Private Const PickEmployee As String = "Pick an employee."

    Private ReadOnly _repository As IEmployeeFamilyRepository
    Private ReadOnly _permissions As IPermissionService
    Private ReadOnly _audit As IAuditService
    Private ReadOnly _today As Func(Of Date)

    Public Sub New(repository As IEmployeeFamilyRepository, permissions As IPermissionService, audit As IAuditService, today As Func(Of Date))
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If today Is Nothing Then Throw New ArgumentNullException(NameOf(today))
        _repository = repository
        _permissions = permissions
        _audit = audit
        _today = today
    End Sub

    Public Function ListFamily(employeeId As Integer) As OperationResult(Of IReadOnlyList(Of FamilyMember))
        If Not CanView() Then Return OperationResult(Of IReadOnlyList(Of FamilyMember)).Fail(NotAllowed)
        If employeeId <= 0 Then Return OperationResult(Of IReadOnlyList(Of FamilyMember)).Fail(PickEmployee)
        Return OperationResult(Of IReadOnlyList(Of FamilyMember)).Ok(_repository.ListFamily(employeeId))
    End Function

    Public Function ListNominees(employeeId As Integer) As OperationResult(Of IReadOnlyList(Of NomineeRow))
        If Not CanView() Then Return OperationResult(Of IReadOnlyList(Of NomineeRow)).Fail(NotAllowed)
        If employeeId <= 0 Then Return OperationResult(Of IReadOnlyList(Of NomineeRow)).Fail(PickEmployee)
        Return OperationResult(Of IReadOnlyList(Of NomineeRow)).Ok(_repository.ListNominees(employeeId))
    End Function

    ''' <summary>Adds a member (FamilyId 0) or changes one. Returns the family id.</summary>
    Public Function SaveFamily(employeeId As Integer, member As FamilyMember) As OperationResult(Of Integer)
        If Not CanEdit() Then Return OperationResult(Of Integer).Fail(NotAllowed)
        If member Is Nothing Then Throw New ArgumentNullException(NameOf(member))
        If employeeId <= 0 Then Return OperationResult(Of Integer).Fail(PickEmployee)
        Dim found = FamilyValidator.Validate(member, _today())
        If found.Count > 0 Then Return OperationResult(Of Integer).Fail(String.Join(" ", found))
        member.Name = member.Name.Trim()
        member.Relation = member.Relation.Trim()
        Dim isNew As Boolean = member.FamilyId = 0
        Dim id As Integer
        Try
            id = _repository.SaveFamily(employeeId, member)
        Catch ex As EmployeeRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim recorded = Record(If(isNew, "AddFamily", "UpdateFamily"), employeeId, $"Family {id}")
        If Not recorded.Succeeded Then Return OperationResult(Of Integer).Fail(AuditFailed("Family member", recorded))
        Return OperationResult(Of Integer).Ok(id)
    End Function

    ''' <summary>Deletes a member. A member who is still a nominee is refused: remove the nomination first.</summary>
    Public Function DeleteFamily(employeeId As Integer, familyId As Integer) As OperationResult
        If Not CanEdit() Then Return OperationResult.Fail(NotAllowed)
        If employeeId <= 0 OrElse familyId <= 0 Then Return OperationResult.Fail("Pick a family member.")
        Try
            _repository.DeleteFamily(familyId)
        Catch ex As EmployeeRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return Record("DeleteFamily", employeeId, $"Family {familyId}")
    End Function

    ''' <summary>Names a family member for a scheme, or changes the share of an existing nomination.</summary>
    Public Function SaveNominee(employeeId As Integer, scheme As NomineeScheme, familyId As Integer, sharePercent As Decimal) As OperationResult
        If Not CanEdit() Then Return OperationResult.Fail(NotAllowed)
        If employeeId <= 0 Then Return OperationResult.Fail(PickEmployee)
        Dim found = FamilyValidator.ValidateNomination(scheme, familyId, sharePercent)
        If found.Count > 0 Then Return OperationResult.Fail(String.Join(" ", found))
        Try
            _repository.SaveNominee(employeeId, scheme, familyId, sharePercent)
        Catch ex As EmployeeRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return Record("SaveNominee", employeeId, $"{scheme} family {familyId} share {sharePercent.ToString(CultureInfo.InvariantCulture)}")
    End Function

    Public Function DeleteNominee(employeeId As Integer, nomineeId As Integer) As OperationResult
        If Not CanEdit() Then Return OperationResult.Fail(NotAllowed)
        If employeeId <= 0 OrElse nomineeId <= 0 Then Return OperationResult.Fail("Pick a nomination.")
        Try
            _repository.DeleteNominee(nomineeId)
        Catch ex As EmployeeRuleException
            Return OperationResult.Fail(ex.Message)
        End Try
        Return Record("DeleteNominee", employeeId, $"Nominee {nomineeId}")
    End Function

    Private Function CanView() As Boolean
        Return _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.View)
    End Function

    Private Function CanEdit() As Boolean
        Return _permissions.IsAllowed(ModuleKey.Employees, PermissionAction.Edit)
    End Function

    Private Function Record(action As String, employeeId As Integer, detail As String) As OperationResult
        Return _audit.Record(New AuditEntry(ModuleKey.Employees, action, "Employee", employeeId.ToString(CultureInfo.InvariantCulture), detail))
    End Function

    Private Shared Function AuditFailed(what As String, recorded As OperationResult) As String
        Return $"{what} was saved, but the audit entry failed: {recorded.Message}"
    End Function
End Class
