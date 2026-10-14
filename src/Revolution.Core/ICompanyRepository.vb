''' <summary>Company storage.</summary>
Public Interface ICompanyRepository
    ''' <summary>The company's licensed plan, or Nothing when the company is missing or inactive.</summary>
    Function GetPlan(companyId As Integer) As PlanTier?
    ''' <summary>The active companies an Associate works on, by name.</summary>
    Function ListForUser(userId As Integer) As IReadOnlyList(Of CompanySummary)
End Interface
