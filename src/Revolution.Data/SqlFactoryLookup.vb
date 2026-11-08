Imports System.Data
Imports Microsoft.Data.SqlClient

''' <summary>Active locations of the current company through mst.usp_Factory_List (ADR 0001, ADR 0008). Tenant context comes from IConnectionFactory.</summary>
Public NotInheritable Class SqlFactoryLookup
    Implements IFactoryLookup

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function ListActive() As IReadOnlyList(Of FactoryChoice) Implements IFactoryLookup.ListActive
        Dim items As New List(Of FactoryChoice)()
        Using conn As SqlConnection = _factory.Open()
            Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, "mst.usp_Factory_List")
                cmd.Parameters.Add("@ActiveOnly", SqlDbType.Bit).Value = True
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        items.Add(New FactoryChoice(reader.GetInt32(reader.GetOrdinal("FactoryID")), reader.GetString(reader.GetOrdinal("FactoryCode")),
                                                    reader.GetString(reader.GetOrdinal("FactoryName"))))
                    End While
                End Using
            End Using
        End Using
        Return items
    End Function
End Class
