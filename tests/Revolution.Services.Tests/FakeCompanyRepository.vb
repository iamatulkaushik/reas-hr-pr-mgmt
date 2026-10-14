Imports Revolution.Core

Public NotInheritable Class FakeCompanyRepository
    Implements ICompanyRepository

    Public ReadOnly Property Plans As New Dictionary(Of Integer, PlanTier)()
    Public ReadOnly Property ByUser As New Dictionary(Of Integer, List(Of CompanySummary))()

    Public Sub Link(userId As Integer, companyId As Integer, name As String, plan As PlanTier)
        If Not ByUser.ContainsKey(userId) Then
            ByUser(userId) = New List(Of CompanySummary)()
        End If
        ByUser(userId).Add(New CompanySummary(companyId, name, plan))
        Plans(companyId) = plan
    End Sub

    Public Function ListForUser(userId As Integer) As IReadOnlyList(Of CompanySummary) Implements ICompanyRepository.ListForUser
        Dim found As List(Of CompanySummary) = Nothing
        If ByUser.TryGetValue(userId, found) Then
            Return found
        End If
        Return New List(Of CompanySummary)()
    End Function

    Public Function GetPlan(companyId As Integer) As PlanTier? Implements ICompanyRepository.GetPlan
        Dim found As PlanTier
        If Plans.TryGetValue(companyId, found) Then
            Return found
        End If
        Return Nothing
    End Function
End Class
