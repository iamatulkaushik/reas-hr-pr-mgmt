Imports Revolution.Core

Public NotInheritable Class FakeLicenceService
    Implements ILicenceService

    Public Property CurrentStatus As LicenceStatus = LicenceStatus.NotLimited()

    Public ReadOnly Property Status As LicenceStatus Implements ILicenceService.Status
        Get
            Return CurrentStatus
        End Get
    End Property

    Public Sub Refresh() Implements ILicenceService.Refresh
    End Sub
End Class
