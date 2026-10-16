''' <summary>
''' Makes and signs a licence (ADR 0018). Defaults: the company cap is the plan's quota, the licence runs one year (the end date is the last
''' day of use), and Security, Licence and Audit are always included so an Associate can never be locked out of managing logins.
''' </summary>
Public NotInheritable Class LicenceIssuer
    Private Shared ReadOnly AlwaysIncluded As ModuleKey() = {ModuleKey.Security, ModuleKey.Licence, ModuleKey.Audit}

    Private Sub New()
    End Sub

    ''' <summary>Must match cfg.PlanQuota in the database (migration 0018).</summary>
    Public Shared Function PlanQuota(plan As PlanTier) As Integer
        Select Case plan
            Case PlanTier.Copper
                Return 5
            Case PlanTier.Silver
                Return 15
            Case PlanTier.Gold
                Return 35
            Case PlanTier.Platinum
                Return 90
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(plan), plan, "Unknown plan.")
        End Select
    End Function

    Public Shared Function Issue(key As SigningKey, associate As String, installationId As Guid, plan As PlanTier,
                                 maxActiveUsers As Integer, maxCompanies As Integer?, modules As IEnumerable(Of ModuleKey),
                                 validFrom As DateOnly, validTo As DateOnly?, issuedBy As String, note As String) As IssuedLicence
        If key Is Nothing Then Throw New ArgumentNullException(NameOf(key))
        If installationId = Guid.Empty Then
            Throw New ArgumentException("The installation id is required.", NameOf(installationId))
        End If
        Dim companies = If(maxCompanies, PlanQuota(plan))
        Dim lastDay = If(validTo, validFrom.AddYears(1).AddDays(-1))
        Dim licensed = AlwaysIncluded.Concat(If(modules, Enumerable.Empty(Of ModuleKey)()))
        Dim grant As New LicenceGrant(Guid.NewGuid(), key.KeyId, issuedBy, DateOnly.FromDateTime(DateTime.UtcNow), installationId,
                                      associate, validFrom, lastDay, plan, licensed, companies, maxActiveUsers, note)
        Dim payload = LicenceFormat.PayloadBytes(grant)
        Return New IssuedLicence(grant, LicenceFormat.Encode(payload, key.Sign(payload)))
    End Function
End Class
