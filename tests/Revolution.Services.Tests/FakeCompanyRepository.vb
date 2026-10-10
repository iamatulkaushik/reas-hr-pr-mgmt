Imports Revolution.Core

Public NotInheritable Class FakeCompanyRepository
    Implements ICompanyRepository

    Public ReadOnly Property Plans As New Dictionary(Of Integer, PlanTier)()

    Public Function GetPlan(companyId As Integer) As PlanTier? Implements ICompanyRepository.GetPlan
        Dim found As PlanTier
        If Plans.TryGetValue(companyId, found) Then
            Return found
        End If
        Return Nothing
    End Function
End Class
