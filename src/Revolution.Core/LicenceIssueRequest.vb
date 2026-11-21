''' <summary>What the Developer enters to issue a licence. The key password is used once to open the key file and is not kept.</summary>
Public NotInheritable Class LicenceIssueRequest
    Public Sub New(associateUserName As String, plan As PlanTier, maxActiveUsers As Integer, maxCompanies As Integer?,
                   validFrom As DateOnly, validTo As DateOnly?, note As String, keyFilePath As String, keyPassword As String)
        Me.AssociateUserName = associateUserName
        Me.Plan = plan
        Me.MaxActiveUsers = maxActiveUsers
        Me.MaxCompanies = maxCompanies
        Me.ValidFrom = validFrom
        Me.ValidTo = validTo
        Me.Note = note
        Me.KeyFilePath = keyFilePath
        Me.KeyPassword = keyPassword
    End Sub

    Public ReadOnly Property AssociateUserName As String
    Public ReadOnly Property Plan As PlanTier
    Public ReadOnly Property MaxActiveUsers As Integer
    ''' <summary>Nothing means the plan's company quota (Copper 5, Silver 15, Gold 35, Platinum 90).</summary>
    Public ReadOnly Property MaxCompanies As Integer?
    Public ReadOnly Property ValidFrom As DateOnly
    ''' <summary>The last day of use. Nothing means one year from ValidFrom.</summary>
    Public ReadOnly Property ValidTo As DateOnly?
    Public ReadOnly Property Note As String
    Public ReadOnly Property KeyFilePath As String
    Public ReadOnly Property KeyPassword As String
End Class
