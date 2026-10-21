Imports Revolution.Core

Public NotInheritable Class FakeLicenceRepository
    Implements ILicenceRepository

    Public Property Installation As InstallationInfo = New InstallationInfo(New Guid("11111111-2222-3333-4444-555555555555"), New DateOnly(2026, 10, 1))
    Public Property Today As DateOnly = New DateOnly(2026, 10, 10)
    Public Property Stored As StoredLicence
    Public Property Current As New List(Of LicenceRecord)()
    Public ReadOnly Property Imported As New List(Of LicenceGrant)()
    ''' <summary>"associate|reason" for every revoke made.</summary>
    Public ReadOnly Property Revoked As New List(Of String)()
    Public ReadOnly Property Reinstated As New List(Of String)()

    Public Function GetInstallation() As InstallationInfo Implements ILicenceRepository.GetInstallation
        Return Installation
    End Function

    Public Function GetServerToday() As DateOnly Implements ILicenceRepository.GetServerToday
        Return Today
    End Function

    Public Function ListCurrent() As OperationResult(Of IReadOnlyList(Of LicenceRecord)) Implements ILicenceRepository.ListCurrent
        Return OperationResult(Of IReadOnlyList(Of LicenceRecord)).Ok(Current)
    End Function

    Public Function Import(grant As LicenceGrant, licenceText As String) As OperationResult Implements ILicenceRepository.Import
        Imported.Add(grant)
        Return OperationResult.Ok()
    End Function

    Public Function Revoke(associateUserName As String, reason As String) As OperationResult Implements ILicenceRepository.Revoke
        Revoked.Add(associateUserName & "|" & reason)
        Return OperationResult.Ok()
    End Function

    Public Function Reinstate(associateUserName As String) As OperationResult Implements ILicenceRepository.Reinstate
        Reinstated.Add(associateUserName)
        Return OperationResult.Ok()
    End Function

    Public Function FindSessionLicence() As StoredLicence Implements ILicenceRepository.FindSessionLicence
        Return Stored
    End Function
End Class
