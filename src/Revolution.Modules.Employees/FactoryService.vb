Imports Revolution.Core

''' <summary>Factory master use cases: permission, validation, storage, audit. Returns OperationResult; never shows a message.</summary>
Public NotInheritable Class FactoryService
    Private ReadOnly _repository As IOrgMasterRepository
    Private ReadOnly _gate As OrgMasterGate

    Public Sub New(repository As IOrgMasterRepository, permissions As IPermissionService, audit As IAuditService, session As ISessionContext)
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        _repository = repository
        _gate = New OrgMasterGate(permissions, audit, session)
    End Sub

    ''' <summary>States for the factory state choice.</summary>
    Public Function ListStates() As OperationResult(Of IReadOnlyList(Of StateItem))
        If Not _gate.CanView() Then Return OrgMasterGate.Denied(Of IReadOnlyList(Of StateItem))()
        Return OperationResult(Of IReadOnlyList(Of StateItem)).Ok(_repository.ListStates())
    End Function

    Public Function List(activeOnly As Boolean) As OperationResult(Of IReadOnlyList(Of FactoryItem))
        If Not _gate.CanView() Then Return OrgMasterGate.Denied(Of IReadOnlyList(Of FactoryItem))()
        Return OperationResult(Of IReadOnlyList(Of FactoryItem)).Ok(_repository.ListFactories(activeOnly))
    End Function

    ''' <summary>Saves a new factory (FactoryId 0) or changes an existing one. Returns the factory id.</summary>
    Public Function Save(item As FactoryItem) As OperationResult(Of Integer)
        Dim isNew As Boolean = item.FactoryId = 0
        If Not _gate.CanSave(isNew) Then Return OrgMasterGate.Denied(Of Integer)()
        Dim found = OrgMasterValidator.Validate(item)
        If found.Count > 0 Then Return OperationResult(Of Integer).Fail(OrgMasterGate.Problems(found))
        item.Code = item.Code.Trim().ToUpperInvariant()
        item.Name = item.Name.Trim()
        Dim id As Integer
        Try
            id = _repository.SaveFactory(item)
        Catch ex As EmployeeRuleException
            Return OperationResult(Of Integer).Fail(ex.Message)
        End Try
        Dim recorded = _gate.Record(isNew, "Factory", id, item.Code)
        If Not recorded.Succeeded Then
            Return OperationResult(Of Integer).Fail($"Factory {item.Code} was saved, but the audit entry failed: {recorded.Message}")
        End If
        Return OperationResult(Of Integer).Ok(id)
    End Function
End Class
