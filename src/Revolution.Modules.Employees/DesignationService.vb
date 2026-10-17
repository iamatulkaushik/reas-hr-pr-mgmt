Imports Revolution.Core

''' <summary>Designation master use cases.</summary>
Public NotInheritable Class DesignationService
    Private ReadOnly _repository As IOrgMasterRepository
    Private ReadOnly _gate As OrgMasterGate

    Public Sub New(repository As IOrgMasterRepository, permissions As IPermissionService, audit As IAuditService, session As ISessionContext)
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        _repository = repository
        _gate = New OrgMasterGate(permissions, audit, session)
    End Sub

    Public Function List(activeOnly As Boolean) As OperationResult(Of IReadOnlyList(Of DesignationItem))
        If Not _gate.CanView() Then Return OrgMasterGate.Denied(Of IReadOnlyList(Of DesignationItem))()
        Return OperationResult(Of IReadOnlyList(Of DesignationItem)).Ok(_repository.ListDesignations(activeOnly))
    End Function

    Public Function Save(item As DesignationItem) As OperationResult(Of Integer)
        Dim isNew As Boolean = item.DesignationId = 0
        If Not _gate.CanSave(isNew) Then Return OrgMasterGate.Denied(Of Integer)()
        Dim found = OrgMasterValidator.Validate(item)
        If found.Count > 0 Then Return OperationResult(Of Integer).Fail(OrgMasterGate.Problems(found))
        item.Code = item.Code.Trim().ToUpperInvariant()
        item.Name = item.Name.Trim()
        Dim id As Integer
        Try
            id = _repository.SaveDesignation(item)
        Catch ex As EmployeeRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim recorded = _gate.Record(isNew, "Designation", id, item.Code)
        If Not recorded.Succeeded Then
            Return OperationResult(Of Integer).Fail($"Designation {item.Code} was saved, but the audit entry failed: {recorded.Message}")
        End If
        Return OperationResult(Of Integer).Ok(id)
    End Function
End Class
