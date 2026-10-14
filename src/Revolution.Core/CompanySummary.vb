''' <summary>A company a user may open: id, name and licensed plan.</summary>
Public NotInheritable Class CompanySummary
    Public Sub New(companyId As Integer, companyName As String, plan As PlanTier)
        Me.CompanyId = companyId
        Me.CompanyName = If(companyName, String.Empty)
        Me.Plan = plan
    End Sub

    Public ReadOnly Property CompanyId As Integer
    Public ReadOnly Property CompanyName As String
    Public ReadOnly Property Plan As PlanTier
End Class
