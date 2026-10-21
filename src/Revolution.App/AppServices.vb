Imports System.IO
Imports System.Security.Cryptography
Imports Revolution.Data
Imports Revolution.Services

''' <summary>
''' Composition root: the one place that builds the shared services and wires them together.
''' Modules receive these through their constructors. Forms never create services themselves.
''' </summary>
Public NotInheritable Class AppServices
    Private Sub New(session As SessionContext, factory As IConnectionFactory, entitlements As IEntitlementService,
                    permissions As IPermissionService, audit As IAuditService, auth As IAuthService,
                    approvals As IApprovalService, associateAdmin As IAssociateAdminService, rates As IStatutoryRateService, crypto As IFieldCrypto,
                    licence As ILicenceService, licenceAdmin As ILicenceAdminService, exporter As ITableExporter)
        Me.Session = session
        Me.Factory = factory
        Me.Entitlements = entitlements
        Me.Permissions = permissions
        Me.Audit = audit
        Me.Auth = auth
        Me.Approvals = approvals
        Me.AssociateAdmin = associateAdmin
        Me.Rates = rates
        Me.Crypto = crypto
        Me.Licence = licence
        Me.LicenceAdmin = licenceAdmin
        Me.Host = New ModuleHost(factory, session, entitlements, permissions, audit, approvals, rates, crypto, exporter)
    End Sub

    Public ReadOnly Property Session As SessionContext
    Public ReadOnly Property Factory As IConnectionFactory
    Public ReadOnly Property Entitlements As IEntitlementService
    Public ReadOnly Property Permissions As IPermissionService
    Public ReadOnly Property Audit As IAuditService
    Public ReadOnly Property Auth As IAuthService
    Public ReadOnly Property Approvals As IApprovalService
    Public ReadOnly Property AssociateAdmin As IAssociateAdminService
    Public ReadOnly Property Rates As IStatutoryRateService
    Public ReadOnly Property Crypto As IFieldCrypto
    Public ReadOnly Property Licence As ILicenceService
    Public ReadOnly Property LicenceAdmin As ILicenceAdminService
    ''' <summary>What every feature module is composed from. Pass it to a module's composition class.</summary>
    Public ReadOnly Property Host As ModuleHost

    ''' <summary>
    ''' Builds everything. Fails with a message the user can act on when the field key file is missing or unreadable:
    ''' the app does not start without it, because protected ids could not be saved or read (ADR 0011).
    ''' </summary>
    Public Shared Function Build(connectionString As String, keyFilePath As String) As OperationResult(Of AppServices)
        Dim keys As FieldKeySet
        Try
            keys = DpapiFieldKeyStore.Load(keyFilePath)
        Catch ex As FileNotFoundException
            Return OperationResult(Of AppServices).Fail($"The field key file was not found at {keyFilePath}. Run Revolution.Setup keys-init (new install) or keys-restore (moved computer).")
        Catch ex As CryptographicException
            Return OperationResult(Of AppServices).Fail("The field key file could not be opened on this computer. Restore it from the password backup with Revolution.Setup keys-restore.")
        End Try

        Dim session As New SessionContext()
        Dim factory As New SqlConnectionFactory(connectionString, session)
        Dim licenceRepository As New SqlLicenceRepository(factory)
        Dim licence As New LicenceService(session, licenceRepository, LicenceKeys.Trusted)
        Dim entitlements As New EntitlementService(session, licence)
        Dim permissions As New PermissionService(session, New SqlPermissionRepository(factory), licence)
        Dim audit As New AuditService(New SqlAuditRepository(factory), session, entitlements)

        Dim hasher As New PasswordHasher()
        Dim auth As New AuthService(New SqlUserRepository(factory), New SqlCompanyRepository(factory),
                                    hasher, session, audit, TimeProvider.System)
        Dim approvals As New ApprovalService(New SqlApprovalRepository(factory), session, permissions, entitlements, audit)
        Dim licenceAdmin As New LicenceAdminService(licenceRepository, permissions, LicenceKeys.Trusted, audit)
        Dim associateAdmin As New AssociateAdminService(New SqlAssociateAdminRepository(factory), permissions, hasher, audit)
        Dim rates As New StatutoryRateService(New SqlStatutoryRateRepository(factory))

        Dim services As New AppServices(session, factory, entitlements, permissions, audit, auth, approvals, associateAdmin, rates, New AesGcmFieldCrypto(keys), licence, licenceAdmin, New NpoiTableExporter())
        Return OperationResult(Of AppServices).Ok(services)
    End Function
End Class
