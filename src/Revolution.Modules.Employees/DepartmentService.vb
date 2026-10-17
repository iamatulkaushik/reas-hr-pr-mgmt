Imports Revolution.Core

''' <summary>Department master use cases. A department keeps the factory it was created under.</summary>
Public NotInheritable Class DepartmentService
    Private ReadOnly _repository As IOrgMasterRepository
    Private ReadOnly _gate As OrgMasterGate

    Public Sub New(repository As IOrgMasterRepository, permissions As IPermissionService, audit As IAuditService, session As ISessionContext)
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        _repository = repository
        _gate = New OrgMasterGate(permissions, audit, session)
    End Sub

    ''' <summary>Pass a factory id to see one factory only, or Nothing for all.</summary>
    Public Function List(factoryId As Integer?, activeOnly As Boolean) As OperationResult(Of IReadOnlyList(Of DepartmentItem))
        If Not _gate.CanView() Then Return OrgMasterGate.Denied(Of IReadOnlyList(Of DepartmentItem))()
        Return OperationResult(Of IReadOnlyList(Of DepartmentItem)).Ok(_repository.ListDepartments(factoryId, activeOnly))
    End Function

    Public Function Save(item As DepartmentItem) As OperationResult(Of Integer)
        Dim isNew As Boolean = item.DepartmentId = 0
        If Not _gate.CanSave(isNew) Then Return OrgMasterGate.Denied(Of Integer)()
        Dim found = OrgMasterValidator.Validate(item)
        If found.Count > 0 Then Return OperationResult(Of Integer).Fail(OrgMasterGate.Problems(found))
        item.Code = item.Code.Trim().ToUpperInvariant()
        item.Name = item.Name.Trim()
        Dim id As Integer
        Try
            id = _repository.SaveDepartment(item)
        Catch ex As EmployeeRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim recorded = _gate.Record(isNew, "Department", id, item.Code)
        If Not recorded.Succeeded Then
            Return OperationResult(Of Integer).Fail($"Department {item.Code} was saved, but the audit entry failed: {recorded.Message}")
        End If
        Return OperationResult(Of Integer).Ok(id)
    End Function
End Class
