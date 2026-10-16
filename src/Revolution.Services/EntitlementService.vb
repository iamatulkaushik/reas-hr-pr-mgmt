''' <summary>Answers plan-feature questions from the signed-in session.</summary>
Public NotInheritable Class EntitlementService
    Implements IEntitlementService

    Private ReadOnly _session As ISessionContext
    Private ReadOnly _licence As ILicenceService

    ''' <summary>With a licence service the plan is the lower of the company's plan and the licence's plan (ADR 0018). Without one, the company's plan.</summary>
    Public Sub New(session As ISessionContext, Optional licence As ILicenceService = Nothing)
        _session = session
        _licence = licence
    End Sub

    Public ReadOnly Property CurrentPlan As PlanTier Implements IEntitlementService.CurrentPlan
        Get
            Dim user = _session.Current
            Return If(user Is Nothing, PlanTier.Copper, EffectivePlan(user))
        End Get
    End Property

    Public Function IsEnabled(feature As PlanFeature) As Boolean Implements IEntitlementService.IsEnabled
        Dim user = _session.Current
        If user Is Nothing Then
            Return False
        End If
        Return PlanFeatureMap.IsIncluded(EffectivePlan(user), feature)
    End Function

    Private Function EffectivePlan(user As UserSession) As PlanTier
        If _licence Is Nothing Then
            Return user.Plan
        End If
        Dim cap = _licence.Status.PlanCap
        If cap.HasValue AndAlso cap.Value < user.Plan Then
            Return cap.Value
        End If
        Return user.Plan
    End Function
End Class
