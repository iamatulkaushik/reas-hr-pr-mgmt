''' <summary>Answers plan-feature questions from the signed-in session.</summary>
Public NotInheritable Class EntitlementService
    Implements IEntitlementService

    Private ReadOnly _session As ISessionContext

    Public Sub New(session As ISessionContext)
        _session = session
    End Sub

    Public ReadOnly Property CurrentPlan As PlanTier Implements IEntitlementService.CurrentPlan
        Get
            Dim user = _session.Current
            Return If(user Is Nothing, PlanTier.Copper, user.Plan)
        End Get
    End Property

    Public Function IsEnabled(feature As PlanFeature) As Boolean Implements IEntitlementService.IsEnabled
        Dim user = _session.Current
        If user Is Nothing Then
            Return False
        End If
        Return PlanFeatureMap.IsIncluded(user.Plan, feature)
    End Function
End Class
