''' <summary>What the licence allows the signed-in user right now. Services ask this; screens ask IPermissionService and IEntitlementService.</summary>
Public Interface ILicenceService
    ''' <summary>Never Nothing. The Developer and a signed-out session are NotLimited.</summary>
    ReadOnly Property Status As LicenceStatus
    ''' <summary>Forget the remembered status, for example after a licence is imported.</summary>
    Sub Refresh()
End Interface
