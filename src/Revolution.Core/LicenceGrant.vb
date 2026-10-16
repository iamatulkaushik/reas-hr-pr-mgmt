''' <summary>
''' What one licence allows one Associate on one installation (ADR 0018). Holds no employee or company data.
''' The end date is the last day of use; the warning and grace periods are worked out from it (LicenceEvaluator).
''' </summary>
Public NotInheritable Class LicenceGrant
    ''' <summary>Same ceiling as the company quota in the database (ADR 0014).</summary>
    Public Const MaxCompaniesCeiling As Integer = 500

    Public Sub New(licenceId As Guid, keyId As String, issuedBy As String, issuedOn As DateOnly, installationId As Guid,
                   associate As String, validFrom As DateOnly, validTo As DateOnly, planCap As PlanTier,
                   modules As IEnumerable(Of ModuleKey), maxCompanies As Integer, maxActiveUsers As Integer,
                   Optional note As String = "")
        RequireText(keyId, NameOf(keyId))
        RequireText(issuedBy, NameOf(issuedBy))
        RequireText(associate, NameOf(associate))
        If validTo.DayNumber < validFrom.DayNumber Then
            Throw New ArgumentException("The end date is before the start date.", NameOf(validTo))
        End If
        If Not System.Enum.IsDefined(GetType(PlanTier), planCap) Then
            Throw New ArgumentException("Unknown plan.", NameOf(planCap))
        End If
        If maxCompanies < 1 OrElse maxCompanies > MaxCompaniesCeiling Then
            Throw New ArgumentException($"Companies must be 1 to {MaxCompaniesCeiling}.", NameOf(maxCompanies))
        End If
        If maxActiveUsers < 1 Then
            Throw New ArgumentException("At least one active user must be allowed.", NameOf(maxActiveUsers))
        End If
        Dim moduleList = If(modules, Enumerable.Empty(Of ModuleKey)()).Distinct().OrderBy(Function(m) CInt(m)).ToList()
        If moduleList.Count = 0 Then
            Throw New ArgumentException("A licence needs at least one module.", NameOf(modules))
        End If
        Me.LicenceId = licenceId
        Me.KeyId = keyId.Trim()
        Me.IssuedBy = issuedBy.Trim()
        Me.IssuedOn = issuedOn
        Me.InstallationId = installationId
        Me.Associate = associate.Trim()
        Me.ValidFrom = validFrom
        Me.ValidTo = validTo
        Me.PlanCap = planCap
        Me.Modules = moduleList
        Me.MaxCompanies = maxCompanies
        Me.MaxActiveUsers = maxActiveUsers
        Me.Note = If(note, String.Empty).Trim()
    End Sub

    Public ReadOnly Property LicenceId As Guid
    ''' <summary>Which trusted public key checks this licence's signature.</summary>
    Public ReadOnly Property KeyId As String
    ''' <summary>The Developer's user name, for the audit trail.</summary>
    Public ReadOnly Property IssuedBy As String
    Public ReadOnly Property IssuedOn As DateOnly
    ''' <summary>Written by first-run into the database; the licence works only there.</summary>
    Public ReadOnly Property InstallationId As Guid
    ''' <summary>The Associate's user name.</summary>
    Public ReadOnly Property Associate As String
    Public ReadOnly Property ValidFrom As DateOnly
    ''' <summary>The last day of use.</summary>
    Public ReadOnly Property ValidTo As DateOnly
    ''' <summary>The highest plan this Associate's companies may use.</summary>
    Public ReadOnly Property PlanCap As PlanTier
    Public ReadOnly Property Modules As IReadOnlyList(Of ModuleKey)
    Public ReadOnly Property MaxCompanies As Integer
    Public ReadOnly Property MaxActiveUsers As Integer
    Public ReadOnly Property Note As String

    Public Function Includes(moduleKey As ModuleKey) As Boolean
        Return Modules.Contains(moduleKey)
    End Function

    Private Shared Sub RequireText(value As String, name As String)
        If String.IsNullOrWhiteSpace(value) Then
            Throw New ArgumentException("A value is required.", name)
        End If
    End Sub
End Class
