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

    Public Function ListForUser(userId As Integer) As IReadOnlyList(Of CompanySummary) Implements ICompanyRepository.ListForUser
        Dim found As New List(Of CompanySummary)()
        Using conn = _factory.Open()
            Using cmd = StoredProcedure.CreateCommand(conn, "sec.usp_UserCompany_List")
                cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = userId
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        found.Add(New CompanySummary(r.GetInt32(r.GetOrdinal("CompanyID")), r.GetString(r.GetOrdinal("CompanyName")),
                                                     CType(CInt(r.GetByte(r.GetOrdinal("PlanTierID"))), PlanTier)))
                    End While
                End Using
            End Using
        End Using
        Return found
    End Function
End Class
