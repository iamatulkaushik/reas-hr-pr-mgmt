''' <summary>Licence storage (ADR 0018). Dates come from SQL Server, never from the PC's clock.</summary>
Public Interface ILicenceRepository
    Function GetInstallation() As InstallationInfo
    ''' <summary>Today as a UTC date, read from SQL Server.</summary>
    Function GetServerToday() As DateOnly
    ''' <summary>The one current licence that governs the signed-in session, or Nothing when there is none.</summary>
    Function FindSessionLicence() As StoredLicence
End Interface
