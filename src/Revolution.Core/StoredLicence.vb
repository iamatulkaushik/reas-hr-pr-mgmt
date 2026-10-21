''' <summary>A licence file as stored for an Associate. The text still has to be verified before anything in it is trusted.</summary>
Public NotInheritable Class StoredLicence
    Public Sub New(associateUserName As String, licenceText As String, Optional revokedOn As DateOnly? = Nothing, Optional revokeReason As String = "")
        Me.AssociateUserName = associateUserName
        Me.LicenceText = licenceText
        Me.RevokedOn = revokedOn
        Me.RevokeReason = If(revokeReason, String.Empty)
    End Sub

    Public ReadOnly Property AssociateUserName As String
    Public ReadOnly Property LicenceText As String
    ''' <summary>The UTC day the Developer revoked this licence, or Nothing.</summary>
    Public ReadOnly Property RevokedOn As DateOnly?
    Public ReadOnly Property RevokeReason As String

    Public ReadOnly Property IsRevoked As Boolean
        Get
            Return RevokedOn.HasValue
        End Get
    End Property
End Class
