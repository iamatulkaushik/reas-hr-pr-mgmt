''' <summary>
''' What governs the signed-in session: no limits, a licence, or the trial (ADR 0018). Built by the licence service.
''' Expired means read-only: view and print only, no export, no create, edit, process or approve.
''' </summary>
Public NotInheritable Class LicenceStatus
    Private Sub New(kind As LicenceKind, state As LicenceState, grant As LicenceGrant, daysLeft As Integer,
                    planCap As PlanTier?, problem As LicenceProblem, problemMessage As String)
        Me.Kind = kind
        Me.State = state
        Me.Grant = grant
        Me.DaysLeft = daysLeft
        Me.PlanCap = planCap
        Me.Problem = problem
        Me.ProblemMessage = problemMessage
    End Sub

    Public ReadOnly Property Kind As LicenceKind
    Public ReadOnly Property State As LicenceState
    ''' <summary>The genuine licence when Kind is Licensed, otherwise Nothing.</summary>
    Public ReadOnly Property Grant As LicenceGrant
    ''' <summary>Days up to and including the end date (or the end of the trial). Negative once it has passed.</summary>
    Public ReadOnly Property DaysLeft As Integer
    ''' <summary>The highest plan allowed. Nothing means no cap.</summary>
    Public ReadOnly Property PlanCap As PlanTier?
    ''' <summary>Why a stored licence was not used (and the trial applies instead). None when there was nothing wrong.</summary>
    Public ReadOnly Property Problem As LicenceProblem
    Public ReadOnly Property ProblemMessage As String

    Public ReadOnly Property IsReadOnly As Boolean
        Get
            Return Kind <> LicenceKind.NotLimited AndAlso (State = LicenceState.Expired OrElse State = LicenceState.Revoked)
        End Get
    End Property

    Public Function AllowsModule(moduleKey As ModuleKey) As Boolean
        If Kind <> LicenceKind.Licensed Then
            Return True
        End If
        Return Grant.Includes(moduleKey)
    End Function

    ''' <summary>A line for the shell, or an empty string when nothing needs saying.</summary>
    Public ReadOnly Property Banner As String
        Get
            If Kind = LicenceKind.NotLimited Then
                Return String.Empty
            End If
            Return If(Kind = LicenceKind.Trial, TrialBanner(), LicenceBanner())
        End Get
    End Property

    Public Shared Function NotLimited() As LicenceStatus
        Return New LicenceStatus(LicenceKind.NotLimited, LicenceState.Valid, Nothing, 0, Nothing, LicenceProblem.None, String.Empty)
    End Function

    Public Shared Function ForLicence(grant As LicenceGrant, state As LicenceState, daysLeft As Integer) As LicenceStatus
        If grant Is Nothing Then Throw New ArgumentNullException(NameOf(grant))
        Return New LicenceStatus(LicenceKind.Licensed, state, grant, daysLeft, grant.PlanCap, LicenceProblem.None, String.Empty)
    End Function

    ''' <summary>The trial: Copper limits while days are left, read-only after. A problem with a stored licence is kept for the Developer to see.</summary>
    Public Shared Function ForTrial(daysLeft As Integer, problem As LicenceProblem, problemMessage As String) As LicenceStatus
        Dim state = If(daysLeft >= 0, LicenceState.Expiring, LicenceState.Expired)
        Return New LicenceStatus(LicenceKind.Trial, state, Nothing, daysLeft, PlanTier.Copper, problem, If(problemMessage, String.Empty))
    End Function

    Private Function TrialBanner() As String
        If State = LicenceState.Expired Then
            Return $"The {LicenceEvaluator.TrialDays}-day trial has ended. The app is read-only: view and print only. Ask the Developer for a licence."
        End If
        Return $"Trial: {DaysLeft} day(s) left. Ask the Developer for a licence."
    End Function

    Private Function LicenceBanner() As String
        If State = LicenceState.Expiring Then
            Return If(DaysLeft = 0, "Your licence ends today.", $"Your licence ends in {DaysLeft} day(s).")
        End If
        If State = LicenceState.Grace Then
            Return $"Your licence ended {-DaysLeft} day(s) ago. The app becomes read-only in {LicenceEvaluator.GraceDays + DaysLeft + 1} day(s)."
        End If
        If State = LicenceState.Expired Then
            Return "Your licence has expired. The app is read-only: view and print only."
        End If
        If State = LicenceState.Revoked Then
            Return "Your licence has been withdrawn. The app is read-only: view and print only. Contact the Developer."
        End If
        Return String.Empty
    End Function
End Class
