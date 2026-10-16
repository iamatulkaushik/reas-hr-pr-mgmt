''' <summary>A current licence row as stored for an Associate (ADR 0018). The text must still be verified before anything is trusted.</summary>
Public NotInheritable Class LicenceRecord
    Public Sub New(associateUserName As String, licenceText As String, validFrom As DateOnly, validTo As DateOnly,
                   planCap As PlanTier, maxCompanies As Integer, maxActiveUsers As Integer)
        Me.AssociateUserName = associateUserName
        Me.LicenceText = licenceText
        Me.ValidFrom = validFrom
        Me.ValidTo = validTo
        Me.PlanCap = planCap
        Me.MaxCompanies = maxCompanies
        Me.MaxActiveUsers = maxActiveUsers
    End Sub

    Public ReadOnly Property AssociateUserName As String
    Public ReadOnly Property LicenceText As String
    Public ReadOnly Property ValidFrom As DateOnly
    Public ReadOnly Property ValidTo As DateOnly
    Public ReadOnly Property PlanCap As PlanTier
    Public ReadOnly Property MaxCompanies As Integer
    Public ReadOnly Property MaxActiveUsers As Integer
End Class
