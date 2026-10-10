''' <summary>Plan features. Screens call this; they never compare plan names.</summary>
Public Interface IEntitlementService
    ReadOnly Property CurrentPlan As PlanTier
    Function IsEnabled(feature As PlanFeature) As Boolean
End Interface
