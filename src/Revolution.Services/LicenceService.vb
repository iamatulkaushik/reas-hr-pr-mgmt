Imports System.Globalization

''' <summary>
''' Works out what governs the signed-in session (ADR 0018): the Associate's one licence when it is genuine, otherwise the 28-day trial.
''' The Developer and a signed-out session are never limited. The licence is verified every time it is read, and every date comes from
''' SQL Server. The result is remembered per user and company and recomputed when either changes or the UTC day changes.
''' </summary>
Public NotInheritable Class LicenceService
    Implements ILicenceService

    Private ReadOnly _session As ISessionContext
    Private ReadOnly _repository As ILicenceRepository
    Private ReadOnly _trustedKeys As IReadOnlyDictionary(Of String, String)
    Private _remembered As LicenceStatus
    Private _rememberedUserId As Integer
    Private _rememberedCompanyId As Integer
    Private _rememberedOn As DateTime
    Private _rememberedAt As DateTime

    ''' <summary>A revoke reaches a running session within this time, without a new sign-in.</summary>
    Private Shared ReadOnly RecheckAfter As TimeSpan = TimeSpan.FromMinutes(5)

    Public Sub New(session As ISessionContext, repository As ILicenceRepository, trustedKeys As IReadOnlyDictionary(Of String, String))
        If session Is Nothing Then Throw New ArgumentNullException(NameOf(session))
        If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
        If trustedKeys Is Nothing Then Throw New ArgumentNullException(NameOf(trustedKeys))
        _session = session
        _repository = repository
        _trustedKeys = trustedKeys
    End Sub

    Public ReadOnly Property Status As LicenceStatus Implements ILicenceService.Status
        Get
            Dim user = _session.Current
            If user Is Nothing OrElse user.Role = UserRole.Developer Then
                Return LicenceStatus.NotLimited()
            End If
            If _remembered Is Nothing OrElse _rememberedUserId <> user.UserId OrElse _rememberedCompanyId <> user.CompanyId _
                OrElse _rememberedOn <> DateTime.UtcNow.Date OrElse DateTime.UtcNow - _rememberedAt >= RecheckAfter Then
                _remembered = Compute()
                _rememberedUserId = user.UserId
                _rememberedCompanyId = user.CompanyId
                _rememberedOn = DateTime.UtcNow.Date
                _rememberedAt = DateTime.UtcNow
            End If
            Return _remembered
        End Get
    End Property

    Public Sub Refresh() Implements ILicenceService.Refresh
        _remembered = Nothing
    End Sub

    Private Function Compute() As LicenceStatus
        Dim today = _repository.GetServerToday()
        Dim installation = _repository.GetInstallation()
        Dim stored = _repository.FindSessionLicence()
        Dim problem = LicenceProblem.None
        Dim message = String.Empty
        If stored IsNot Nothing Then
            Dim check = LicenceVerifier.Verify(stored.LicenceText, _trustedKeys, installation.Id)
            If Not check.Succeeded Then
                problem = check.Problem
                message = check.Message
            ElseIf Not String.Equals(check.Grant.Associate, stored.AssociateUserName, StringComparison.OrdinalIgnoreCase) Then
                problem = LicenceProblem.WrongAssociate
                message = "This licence was issued to a different Associate."
            Else
                Dim state = If(stored.IsRevoked, LicenceState.Revoked, LicenceEvaluator.StateOn(check.Grant, today))
                If state <> LicenceState.NotYetValid Then
                    Return LicenceStatus.ForLicence(check.Grant, state, LicenceEvaluator.DaysLeft(check.Grant, today))
                End If
                problem = LicenceProblem.NotYetValid
                message = "The licence starts on " & check.Grant.ValidFrom.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture) & "."
            End If
        End If
        Dim trialDaysLeft = LicenceEvaluator.TrialDays - (today.DayNumber - installation.CreatedOn.DayNumber)
        Return LicenceStatus.ForTrial(trialDaysLeft, problem, message)
    End Function
End Class
