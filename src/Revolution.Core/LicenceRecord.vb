''' <summary>A current licence row as stored for an Associate (ADR 0018). The text must still be verified before anything is trusted.</summary>
Public NotInheritable Class LicenceRecord
    Public Sub New(associateUserName As String, licenceText As String, validFrom As DateOnly, validTo As DateOnly,
                   planCap As PlanTier, maxCompanies As Integer, maxActiveUsers As Integer,
                   Optional revokedOn As DateOnly? = Nothing, Optional revokeReason As String = "")
        Me.AssociateUserName = associateUserName
        Me.LicenceText = licenceText
        Me.ValidFrom = validFrom
        Me.ValidTo = validTo
        Me.PlanCap = planCap
        Me.MaxCompanies = maxCompanies
        Me.MaxActiveUsers = maxActiveUsers
        Me.RevokedOn = revokedOn
        Me.RevokeReason = If(revokeReason, String.Empty)
    End Sub

    Public ReadOnly Property AssociateUserName As String
    Public ReadOnly Property LicenceText As String
    Public ReadOnly Property ValidFrom As DateOnly
    Public ReadOnly Property ValidTo As DateOnly
    Public ReadOnly Property PlanCap As PlanTier
    Public ReadOnly Property MaxCompanies As Integer
    Public ReadOnly Property MaxActiveUsers As Integer
    ''' <summary>The UTC day the Developer revoked this licence, or Nothing.</summary>
    Public ReadOnly Property RevokedOn As DateOnly?
    Public ReadOnly Property RevokeReason As String

    Public ReadOnly Property IsRevoked As Boolean
        Get
            Return RevokedOn.HasValue
        End Get
    End Property
End Class
