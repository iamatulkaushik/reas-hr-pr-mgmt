''' <summary>
''' The shared services a feature module is built from. A module writes one public composition class that takes a ModuleHost and
''' returns its own services (its repositories use Factory and the StoredProcedure helpers, ADR 0008). Revolution.App builds the host
''' once in AppServices and calls each module's composition class, so adding a module never needs a change to shared code.
''' </summary>
Public NotInheritable Class ModuleHost
    Public Sub New(factory As IConnectionFactory, session As ISessionContext, entitlements As IEntitlementService,
                   permissions As IPermissionService, audit As IAuditService, approvals As IApprovalService,
                   rates As IStatutoryRateService, crypto As IFieldCrypto)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If audit Is Nothing Then Throw New ArgumentNullException(NameOf(audit))
        If approvals Is Nothing Then Throw New ArgumentNullException(NameOf(approvals))
        If rates Is Nothing Then Throw New ArgumentNullException(NameOf(rates))
        If crypto Is Nothing Then Throw New ArgumentNullException(NameOf(crypto))
        Me.Factory = factory
        Me.Session = session
        Me.Entitlements = entitlements
        Me.Permissions = permissions
        Me.Audit = audit
        Me.Approvals = approvals
        Me.Rates = rates
        Me.Crypto = crypto
    End Sub

    ''' <summary>Opens connections with the tenant context applied.</summary>
    Public ReadOnly Property Factory As IConnectionFactory
    Public ReadOnly Property Session As ISessionContext
    ''' <summary>Plan features. Ask this, never a plan name (ADR 0003).</summary>
    Public ReadOnly Property Entitlements As IEntitlementService
    ''' <summary>Role permissions. Ask this, never a role name (ADR 0003).</summary>
    Public ReadOnly Property Permissions As IPermissionService
    Public ReadOnly Property Audit As IAuditService
    ''' <summary>Dual approval hook (ADR 0009). Payroll runs submit here.</summary>
    Public ReadOnly Property Approvals As IApprovalService
    ''' <summary>Effective-dated statutory rates. Fails when a rate is missing or not signed off.</summary>
    Public ReadOnly Property Rates As IStatutoryRateService
    ''' <summary>Encrypts and decrypts protected ids (ADR 0011).</summary>
    Public ReadOnly Property Crypto As IFieldCrypto
End Class
