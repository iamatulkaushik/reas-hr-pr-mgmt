Imports System.IO
Imports System.Text.Json
Imports System.Text.RegularExpressions
Imports Xunit

''' <summary>The layout files of the report catalogue: clean Host reports whose every field and parameter is declared (ADR 0028).</summary>
Public Class ReportLayoutFilesTests
    Private Shared ReadOnly Token As New Regex("\{([^{}]*)\}", RegexOptions.Compiled)
    Private Shared ReadOnly Identifier As New Regex("(?<![@\w'])([A-Za-z_]\w*)(?!\w*\()", RegexOptions.Compiled)
    Private Shared ReadOnly ParameterRef As New Regex("@(\w+)", RegexOptions.Compiled)
    Private Shared ReadOnly Quoted As New Regex("'[^']*'", RegexOptions.Compiled)

    Public Shared Function Layouts() As IEnumerable(Of Object())
        Return ReportCatalog.All.Select(Function(entry) New Object() {entry.LayoutFile}).ToList()
    End Function

    <Theory>
    <MemberData(NameOf(Layouts))>
    Public Sub LayoutIsAHostReportWithNoQueryOrConnection(layoutFile As String)
        Using doc As JsonDocument = Open(layoutFile)
            Dim source As JsonElement = doc.RootElement.GetProperty("DataSource")
            Assert.Equal("Host", source.GetProperty("Provider").GetString())
            Assert.Equal(String.Empty, source.GetProperty("Query").GetString())
            Assert.Equal(String.Empty, source.GetProperty("ConnectionString").GetString())
        End Using
    End Sub

    <Theory>
    <MemberData(NameOf(Layouts))>
    Public Sub EveryFieldAndParameterUsedIsDeclared(layoutFile As String)
        Using doc As JsonDocument = Open(layoutFile)
            Dim fields As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each item As JsonElement In doc.RootElement.GetProperty("DataSource").GetProperty("Fields").EnumerateArray()
                fields.Add(item.GetProperty("Name").GetString())
            Next
            Dim parameters As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each item As JsonElement In doc.RootElement.GetProperty("Parameters").EnumerateArray()
                parameters.Add(item.GetProperty("Name").GetString())
            Next
            Dim missing As New List(Of String)()
            For Each content As String In TextsOf(doc.RootElement)
                CollectMissing(content, fields, parameters, missing)
            Next
            Assert.True(missing.Count = 0, "Not declared in " & layoutFile & ": " & String.Join(", ", missing))
        End Using
    End Sub

    <Fact>
    Public Sub CatalogKeysAndLayoutsAreUniqueAndUseRptProcedures()
        Assert.Equal(ReportCatalog.All.Count, ReportCatalog.All.Select(Function(entry) entry.Key).Distinct().Count())
        For Each entry As ReportEntry In ReportCatalog.All
            Assert.StartsWith("rpt.", entry.Procedure, StringComparison.Ordinal)
        Next
    End Sub

    <Fact>
    Public Sub RunInfoShowsThePeriodAndTheDraftMark()
        Dim info As New ReportRunInfo(7, New Date(2026, 10, 1), New Date(2026, 10, 31), True, "Acme")
        Assert.Equal("01-10-2026 to 31-10-2026", info.PeriodText)
        Assert.Equal("01-10-2026 to 31-10-2026 (draft)", info.ToString())
    End Sub

    Private Shared Sub CollectMissing(content As String, fields As HashSet(Of String), parameters As HashSet(Of String), missing As List(Of String))
        For Each match As Match In Token.Matches(content)
            Dim inner As String = Quoted.Replace(match.Groups(1).Value, String.Empty)
            For Each hit As Match In Identifier.Matches(inner)
                Dim word As String = hit.Groups(1).Value
                If word <> "Page" AndAlso word <> "PageCount" AndAlso Not fields.Contains(word) Then missing.Add(word)
            Next
        Next
        For Each reference As Match In ParameterRef.Matches(content)
            If Not parameters.Contains(reference.Groups(1).Value) Then missing.Add("@" & reference.Groups(1).Value)
        Next
    End Sub

    Private Shared Iterator Function TextsOf(element As JsonElement) As IEnumerable(Of String)
        Select Case element.ValueKind
            Case JsonValueKind.Object
                For Each prop As JsonProperty In element.EnumerateObject()
                    If prop.NameEquals("Text") AndAlso prop.Value.ValueKind = JsonValueKind.String Then
                        Yield prop.Value.GetString()
                    Else
                        For Each inner As String In TextsOf(prop.Value)
                            Yield inner
                        Next
                    End If
                Next
            Case JsonValueKind.Array
                For Each item As JsonElement In element.EnumerateArray()
                    For Each inner As String In TextsOf(item)
                        Yield inner
                    Next
                Next
        End Select
    End Function

    Private Shared Function Open(layoutFile As String) As JsonDocument
        Dim layoutPath As String = System.IO.Path.Combine(LayoutsFolder(), layoutFile)
        Assert.True(System.IO.File.Exists(layoutPath), layoutPath & " is missing.")
        Return JsonDocument.Parse(System.IO.File.ReadAllText(layoutPath))
    End Function

    Private Shared Function LayoutsFolder() As String
        Dim dir As DirectoryInfo = New DirectoryInfo(AppContext.BaseDirectory)
        While dir IsNot Nothing AndAlso Not System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "Revolution.slnx"))
            dir = dir.Parent
        End While
        If dir Is Nothing Then Throw New InvalidOperationException("Revolution.slnx was not found above the test folder.")
        Return System.IO.Path.Combine(dir.FullName, "reports", "layouts")
    End Function
End Class
