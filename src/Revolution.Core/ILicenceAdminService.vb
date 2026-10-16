''' <summary>The Developer's licence screen: the installation id, every Associate's current licence, and importing a new one (ADR 0018).</summary>
Public Interface ILicenceAdminService
    ''' <summary>The id a licence must be issued for. Not secret.</summary>
    Function GetInstallationId() As Guid
    Function ListCurrent() As OperationResult(Of IReadOnlyList(Of LicenceOverview))
    ''' <summary>Verifies the licence text, then stores it as the Associate's current licence. Needs create rights on the Licence module.</summary>
    Function Import(licenceText As String) As OperationResult
End Interface
