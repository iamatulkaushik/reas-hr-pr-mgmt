Imports System.IO
Imports System.Text.Json
Imports System.Text.RegularExpressions
Imports Revolution.Modules.Payroll
Imports Xunit

''' <summary>
''' Checks the ReportStudio layout files in reports/layouts without running ReportStudio: each is a clean Host report (no query, no connection string), every field and
''' parameter a text or a visibility rule uses is declared, everything sits inside the page, no form number or section reference is printed as text (CR rule for
''' certificate layouts), and the payslip layout's fields are exactly the columns PayslipReportRows supplies.
''' </summary>
Public Class PayrollLayoutFilesTests
    Private Shared ReadOnly BuiltIns As String() = {"and", "or", "not", "true", "false", "page", "pagecount", "totalpages"}
    Private Shared ReadOnly Banned As String() = {"section", "chapter", "assessment year", "form 1", "form no"}

    Public Shared ReadOnly Property LayoutNames As IEnumerable(Of Object())
        Get
            Return {"payslip.rsr", "wage-register.rsr", "payroll-summary.rsr"}.Select(Function(n) New Object() {n})
        End Get
    End Property

    Private Shared Function LayoutsFolder() As String
        Dim folder = New DirectoryInfo(AppContext.BaseDirectory)
        Do While folder IsNot Nothing
            If File.Exists(Path.Combine(folder.FullName, "Revolution.slnx")) Then Return Path.Combine(folder.FullName, "reports", "layouts")
            folder = folder.Parent
        Loop
        Throw New InvalidOperationException("Revolution.slnx was not found above the test folder, so the layout files cannot be read.")
    End Function

    Private Shared Function Load(name As String) As JsonDocument
        Return JsonDocument.Parse(File.ReadAllText(Path.Combine(LayoutsFolder(), name)))
    End Function

    Private Shared Function Names(items As JsonElement) As HashSet(Of String)
        Return New HashSet(Of String)(items.EnumerateArray().Select(Function(e) e.GetProperty("Name").GetString()), StringComparer.Ordinal)
    End Function

    Private Shared Function Bands(root As JsonElement) As List(Of JsonElement)
        Dim list As New List(Of JsonElement)()
        For Each key In {"ReportHeader", "PageHeader", "Detail", "ReportFooter", "PageFooter"}
            list.Add(root.GetProperty(key))
        Next
        For Each g In root.GetProperty("Groups").EnumerateArray()
            list.Add(g.GetProperty("Header"))
            list.Add(g.GetProperty("Footer"))
        Next
        Return list
    End Function

    Private Shared Function Expressions(item As JsonElement) As List(Of String)
        Dim found As New List(Of String)()
        If item.GetProperty("Kind").GetString() = "Text" Then
            Dim text As String = item.GetProperty("Text").GetString().Replace("{{", String.Empty).Replace("}}", String.Empty)
            For Each m As Match In Regex.Matches(text, "\{(.*?)\}")
                found.Add(m.Groups(1).Value)
            Next
        End If
        Dim vis As JsonElement = Nothing
        If item.TryGetProperty("VisibleWhen", vis) AndAlso Not String.IsNullOrEmpty(vis.GetString()) Then found.Add(vis.GetString())
        Return found
    End Function

    <Theory>
    <MemberData(NameOf(LayoutNames))>
    Public Sub Layout_IsACleanHostReport(name As String)
        Using doc = Load(name)
            Dim ds = doc.RootElement.GetProperty("DataSource")
            Assert.Equal("Host", ds.GetProperty("Provider").GetString())
            Assert.Equal(String.Empty, ds.GetProperty("Query").GetString())
            Assert.Equal(String.Empty, ds.GetProperty("ConnectionString").GetString())
        End Using
    End Sub

    <Theory>
    <MemberData(NameOf(LayoutNames))>
    Public Sub Layout_EveryFieldAndParameterUsedIsDeclared(name As String)
        Using doc = Load(name)
            Dim root = doc.RootElement
            Dim fields = Names(root.GetProperty("DataSource").GetProperty("Fields"))
            Dim params = Names(root.GetProperty("Parameters"))
            For Each b In Bands(root)
                For Each item In b.GetProperty("Items").EnumerateArray()
                    For Each expr In Expressions(item)
                        Dim cleaned As String = Regex.Replace(expr, "'[^']*'", String.Empty)
                        For Each m As Match In Regex.Matches(cleaned, "@(\w+)")
                            Assert.True(params.Contains(m.Groups(1).Value), $"{name}: unknown parameter @{m.Groups(1).Value} in {expr}")
                        Next
                        Dim noParams As String = Regex.Replace(cleaned, "@\w+", String.Empty)
                        For Each m As Match In Regex.Matches(noParams, "\b([A-Za-z_]\w*)\b(?!\s*\()")
                            Dim word As String = m.Groups(1).Value
                            If BuiltIns.Contains(word.ToLowerInvariant()) Then Continue For
                            Assert.True(fields.Contains(word), $"{name}: unknown field {word} in {expr}")
                        Next
                    Next
                Next
            Next
            For Each g In root.GetProperty("Groups").EnumerateArray()
                Assert.Contains(g.GetProperty("Expression").GetString(), fields)
            Next
            For Each s In root.GetProperty("SortBy").EnumerateArray()
                Assert.Contains(s.GetString(), fields)
            Next
        End Using
    End Sub

    <Theory>
    <MemberData(NameOf(LayoutNames))>
    Public Sub Layout_EverythingSitsInsideThePage(name As String)
        Using doc = Load(name)
            Dim root = doc.RootElement
            Dim page = root.GetProperty("Page")
            Dim width As Double = page.GetProperty("Width").GetDouble() - page.GetProperty("MarginLeft").GetDouble() - page.GetProperty("MarginRight").GetDouble()
            For Each b In Bands(root)
                Dim height As Double = b.GetProperty("Height").GetDouble()
                For Each item In b.GetProperty("Items").EnumerateArray()
                    Dim x As Double = item.GetProperty("X").GetDouble()
                    Dim y As Double = item.GetProperty("Y").GetDouble()
                    Assert.True(x >= 0 AndAlso x + item.GetProperty("Width").GetDouble() <= width + 0.5, $"{name}: an item runs past the page width at x={x}")
                    Assert.True(y >= 0 AndAlso y + item.GetProperty("Height").GetDouble() <= height + 0.5, $"{name}: an item runs past its band at y={y}")
                Next
            Next
        End Using
    End Sub

    <Theory>
    <MemberData(NameOf(LayoutNames))>
    Public Sub Layout_PrintsNoFormNumberOrSectionReference(name As String)
        Using doc = Load(name)
            For Each b In Bands(doc.RootElement)
                For Each item In b.GetProperty("Items").EnumerateArray()
                    If item.GetProperty("Kind").GetString() <> "Text" Then Continue For
                    Dim text As String = item.GetProperty("Text").GetString().ToLowerInvariant()
                    For Each word In Banned
                        Assert.False(text.Contains(word), $"{name}: text '{text}' contains '{word}'")
                    Next
                Next
            Next
        End Using
    End Sub

    <Fact>
    Public Sub PayslipLayout_FieldsAreExactlyTheColumnsPayslipReportRowsSupplies()
        Dim row As New PayslipRow With {.RunId = 1, .RunStatus = PayrollRunStatus.Approved, .EmployeeId = 1, .Earnings = New List(Of PayslipEarning) From {New PayslipEarning("B", "Basic", 1, 1D)}}
        Dim slip As New Payslip(row, New PayslipIds("1", "2", "3", False), "x", False)
        Dim supplied As New HashSet(Of String)(PayslipReportRows.Build({slip})(0).Keys, StringComparer.Ordinal)
        Using doc = Load("payslip.rsr")
            Dim declared = Names(doc.RootElement.GetProperty("DataSource").GetProperty("Fields"))
            Assert.True(supplied.SetEquals(declared), "Missing in the layout: " & String.Join(", ", supplied.Except(declared)) & ". Missing in the rows: " & String.Join(", ", declared.Except(supplied)))
        End Using
    End Sub
End Class
