Imports System.Data

Public NotInheritable Class SqlCompanyRepository
    Implements ICompanyRepository

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        _factory = factory
    End Sub

    Public Function GetPlan(companyId As Integer) As PlanTier? Implements ICompanyRepository.GetPlan
        Dim result As PlanTier? = Nothing
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "mst.usp_Company_GetPlan")
                cmd.Parameters.Add("@CompanyID", SqlDbType.Int).Value = companyId
                Dim raw = cmd.ExecuteScalar()
                If raw IsNot Nothing AndAlso raw IsNot DBNull.Value Then
                    result = CType(Convert.ToInt32(raw), PlanTier)
                End If
            End Using
        End Using
        Return result
    End Function
End Class
