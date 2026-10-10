''' <summary>Writes audit rows. Security events are always kept; other modules only when the plan has FullAuditTrail.</summary>
Public NotInheritable Class AuditService
    Implements IAuditService

    Private ReadOnly _repository As IAuditRepository
    Private ReadOnly _session As ISessionContext
    Private ReadOnly _entitlements As IEntitlementService

    Public Sub New(repository As IAuditRepository, session As ISessionContext, entitlements As IEntitlementService)
        _repository = repository
        _session = session
        _entitlements = entitlements
    End Sub

    Public Function Record(entry As AuditEntry) As OperationResult Implements IAuditService.Record
        If entry Is Nothing Then
            Throw New ArgumentNullException(NameOf(entry))
        End If
        If entry.[Module] <> ModuleKey.Security AndAlso Not _entitlements.IsEnabled(PlanFeature.FullAuditTrail) Then
            Return OperationResult.Ok()
        End If
        Dim userId As Integer? = Nothing
        Dim companyId As Integer? = Nothing
        Dim user = _session.Current
        If user IsNot Nothing Then
            userId = user.UserId
            If user.Role <> UserRole.Developer Then
                companyId = user.CompanyId
            End If
        End If
        _repository.Insert(entry, userId, companyId)
        Return OperationResult.Ok()
    End Function
End Class
