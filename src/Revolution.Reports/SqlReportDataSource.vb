Imports System.Globalization
Imports Microsoft.Data.SqlClient
Imports Revolution.Data

''' <summary>
''' Runs the rpt procedures on the tenant-safe connection (ADR 0012) and gives ReportStudio the first result set as rows. Only procedures are run;
''' no query text comes from a layout file (ADR 0028).
''' </summary>
Public NotInheritable Class SqlReportDataSource
    Implements IReportDataSource

    Private Const NoCompany As Integer = 50002
    Private Const RunNotFound As Integer = 50311
    Private Const ApprovedStatus As Integer = 4

    Private ReadOnly _factory As IConnectionFactory

    Public Sub New(factory As IConnectionFactory)
        If factory Is Nothing Then Throw New ArgumentNullException(NameOf(factory))
        _factory = factory
    End Sub

    Public Function ListRuns() As IReadOnlyList(Of ReportRunInfo) Implements IReportDataSource.ListRuns
        Dim runs As New List(Of ReportRunInfo)()
        For Each row As IReadOnlyDictionary(Of String, Object) In Execute("rpt.usp_RunList_Get")
            runs.Add(New ReportRunInfo(AsInt(row, "RunID"), AsDate(row, "PeriodFrom"), AsDate(row, "PeriodTo"),
                                       AsInt(row, "RunStatusID") < ApprovedStatus, AsText(row, "CompanyName")))
        Next
        Return runs
    End Function

    Public Function GetRows(entry As ReportEntry, choice As ReportChoice) As IReadOnlyList(Of IReadOnlyDictionary(Of String, Object)) Implements IReportDataSource.GetRows
        If entry Is Nothing Then Throw New ArgumentNullException(NameOf(entry))
        If choice Is Nothing Then Throw New ArgumentNullException(NameOf(choice))
        Select Case entry.Input
            Case ReportInput.PayrollRun
                Return Execute(entry.Procedure, Arg("@RunID", choice.RunId))
            Case ReportInput.Month
                Return Execute(entry.Procedure, Arg("@PeriodStart", New Date(choice.Month.Year, choice.Month.Month, 1)))
            Case Else
                Return Execute(entry.Procedure, Arg("@IncludeExited", choice.IncludeExited))
        End Select
    End Function

    Private Function Execute(procedure As String, ParamArray values As KeyValuePair(Of String, Object)()) As IReadOnlyList(Of IReadOnlyDictionary(Of String, Object))
        Dim rows As New List(Of IReadOnlyDictionary(Of String, Object))()
        Try
            Using conn As SqlConnection = _factory.Open()
                Using cmd As SqlCommand = StoredProcedure.CreateCommand(conn, procedure)
                    For Each item As KeyValuePair(Of String, Object) In values
                        cmd.Parameters.AddWithValue(item.Key, item.Value)
                    Next
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            rows.Add(ReadRow(reader))
                        End While
                    End Using
                End Using
            End Using
        Catch ex As SqlException When ex.Number = NoCompany OrElse ex.Number = RunNotFound
            Throw New ReportDataException(ex.Message, ex)
        Catch ex As SqlException
            Throw New ReportDataException("The report data could not be read.", ex)
        End Try
        Return rows
    End Function

    Private Shared Function Arg(name As String, value As Object) As KeyValuePair(Of String, Object)
        Return New KeyValuePair(Of String, Object)(name, value)
    End Function

    Private Shared Function ReadRow(reader As SqlDataReader) As IReadOnlyDictionary(Of String, Object)
        Dim row As New Dictionary(Of String, Object)(reader.FieldCount, StringComparer.OrdinalIgnoreCase)
        For index As Integer = 0 To reader.FieldCount - 1
            row(reader.GetName(index)) = Normalise(reader.GetValue(index))
        Next
        Return row
    End Function

    ''' <summary>Null for a database null; small whole numbers as Integer so the report engine reads every number the same way.</summary>
    Private Shared Function Normalise(value As Object) As Object
        If value Is Nothing OrElse TypeOf value Is DBNull Then Return Nothing
        If TypeOf value Is Byte Then Return CInt(CByte(value))
        If TypeOf value Is Short Then Return CInt(CShort(value))
        Return value
    End Function

    Private Shared Function AsInt(row As IReadOnlyDictionary(Of String, Object), column As String) As Integer
        Return Convert.ToInt32(row(column), CultureInfo.InvariantCulture)
    End Function

    Private Shared Function AsDate(row As IReadOnlyDictionary(Of String, Object), column As String) As Date
        Return Convert.ToDateTime(row(column), CultureInfo.InvariantCulture)
    End Function

    Private Shared Function AsText(row As IReadOnlyDictionary(Of String, Object), column As String) As String
        Return Convert.ToString(row(column), CultureInfo.InvariantCulture)
    End Function
End Class
