Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>Shared database plumbing for the Employees repositories: rule-error translation and nullable column readers.</summary>
Friend NotInheritable Class EmployeeSql
    Private Sub New()
    End Sub

    ''' <summary>Runs database work. A rule error becomes EmployeeRuleException; any other error (including 50002, no company) surfaces unchanged.</summary>
    Friend Shared Sub Run(work As Action)
        Try
            work()
        Catch ex As SqlException When EmployeeErrorMap.Translate(ex.Number, ex.Message) IsNot Nothing
            Throw New EmployeeRuleException(EmployeeErrorMap.Translate(ex.Number, ex.Message), ex)
        End Try
    End Sub

    ''' <summary>Runs one stored procedure that returns no rows.</summary>
    Friend Shared Sub Execute(factory As IConnectionFactory, procedure As String, bind As Action(Of SqlCommand))
        Run(Sub()
                Using conn = factory.Open()
                    Using cmd = StoredProcedure.CreateCommand(conn, procedure)
                        bind(cmd)
                        cmd.ExecuteNonQuery()
                    End Using
                End Using
            End Sub)
    End Sub

    Friend Shared Function OptText(reader As SqlDataReader, column As String) As String
        Dim i = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(i), String.Empty, reader.GetString(i))
    End Function

    Friend Shared Function OptDate(reader As SqlDataReader, column As String) As Date?
        Dim i = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(i), CType(Nothing, Date?), reader.GetDateTime(i))
    End Function

    Friend Shared Function OptByte(reader As SqlDataReader, column As String) As Integer?
        Dim i = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(i), CType(Nothing, Integer?), CInt(reader.GetByte(i)))
    End Function

    Friend Shared Function OptBytes(reader As SqlDataReader, column As String) As Byte()
        Dim i = reader.GetOrdinal(column)
        Return If(reader.IsDBNull(i), Nothing, reader.GetFieldValue(Of Byte())(i))
    End Function
End Class
