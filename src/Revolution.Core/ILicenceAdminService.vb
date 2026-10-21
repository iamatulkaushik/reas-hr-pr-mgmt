''' <summary>The Developer's licence screen: the installation id, every Associate's current licence, and importing a new one (ADR 0018).</summary>
Public Interface ILicenceAdminService
    ''' <summary>The id a licence must be issued for. Not secret.</summary>
    Function GetInstallationId() As Guid
    Function ListCurrent() As OperationResult(Of IReadOnlyList(Of LicenceOverview))
    ''' <summary>Verifies the licence text, then stores it as the Associate's current licence. Needs create rights on the Licence module.</summary>
    Function Import(licenceText As String) As OperationResult
    ''' <summary>
    ''' Revokes the Associate's current licence. The Associate and the Associate's Owners and Operators become read-only (view and print only)
    ''' within a few minutes, with no grace period. A reason is required. Needs edit rights on the Licence module.
    ''' </summary>
    Function Revoke(associateUserName As String, reason As String) As OperationResult
    ''' <summary>Undoes a revoke. Needs edit rights on the Licence module.</summary>
    Function Reinstate(associateUserName As String) As OperationResult
End Interface
