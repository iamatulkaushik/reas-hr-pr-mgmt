''' <summary>One business event to record. The audit service adds user, company and time from the session.</summary>
Public NotInheritable Class AuditEntry
    Public Sub New(moduleKey As ModuleKey, action As String, entityName As String, entityId As String, Optional details As String = "")
        If String.IsNullOrWhiteSpace(action) Then
            Throw New ArgumentException("An audit entry needs an action.", NameOf(action))
        End If
        If String.IsNullOrWhiteSpace(entityName) Then
            Throw New ArgumentException("An audit entry needs an entity name.", NameOf(entityName))
        End If
        Me.[Module] = moduleKey
        Me.Action = action
        Me.EntityName = entityName
        Me.EntityId = If(entityId, String.Empty)
        Me.Details = If(details, String.Empty)
    End Sub

    Public ReadOnly Property [Module] As ModuleKey
    Public ReadOnly Property Action As String
    Public ReadOnly Property EntityName As String
    Public ReadOnly Property EntityId As String
    Public ReadOnly Property Details As String
End Class
