Imports System.IO
Imports System.Text.RegularExpressions
Imports Xunit

''' <summary>
''' ADR 0024. The migrations must build an empty database from 0001 to the last script, in file-name order. These tests read the script files only (no database)
''' and catch the two mistakes that a database built step by step never shows: a script that changes a table before a later script has created it, and a patch script
''' whose procedure is put back to an older version by a later script.
''' </summary>
Public Class MigrationOrderTests
    Private Const TableName As String = "(\[?\w+\]?\.\[?\w+\]?)"

    Private Shared Function MigrationsFolder() As String
        Dim dir As DirectoryInfo = New DirectoryInfo(AppContext.BaseDirectory)
        While dir IsNot Nothing
            Dim candidate As String = Path.Combine(dir.FullName, "database", "migrations")
            If Directory.Exists(candidate) Then
                Return candidate
            End If
            dir = dir.Parent
        End While
        Throw New DirectoryNotFoundException("database/migrations was not found above " & AppContext.BaseDirectory)
    End Function

    Private Shared Function Scripts() As List(Of KeyValuePair(Of String, String))
        Dim result As New List(Of KeyValuePair(Of String, String))()
        Dim files As List(Of String) = Directory.GetFiles(MigrationsFolder(), "*.sql").OrderBy(Function(f) Path.GetFileName(f), StringComparer.Ordinal).ToList()
        For Each scriptPath As String In files
            Dim text As String = File.ReadAllText(scriptPath).Replace(vbCrLf, vbLf)
            text = Regex.Replace(text, "/\*.*?\*/", String.Empty, RegexOptions.Singleline)
            text = Regex.Replace(text, "--[^\n]*", String.Empty)
            result.Add(New KeyValuePair(Of String, String)(Path.GetFileName(scriptPath), text))
        Next
        Return result
    End Function

    Private Shared Function Plain(name As String) As String
        Return name.Replace("[", String.Empty).Replace("]", String.Empty).ToLowerInvariant()
    End Function

    Private Shared Function Batches(text As String) As String()
        Return Regex.Split(text, "^\s*GO\s*$", RegexOptions.Multiline Or RegexOptions.IgnoreCase)
    End Function

    <Fact>
    Public Sub NoScriptChangesATableBeforeItExists()
        Dim patterns As String() = {
            "ALTER\s+TABLE\s+" & TableName, "REFERENCES\s+" & TableName, "\bON\s+" & TableName & "\s*\(", "ADD\s+FILTER\s+PREDICATE[^)]*\)\s+ON\s+" & TableName,
            "\bINSERT\s+(?:INTO\s+)?" & TableName, "\bUPDATE\s+" & TableName & "\s+SET", "\bFROM\s+" & TableName, "\bJOIN\s+" & TableName}
        Dim created As New HashSet(Of String)()
        Dim problems As New List(Of String)()
        For Each script As KeyValuePair(Of String, String) In Scripts()
            Dim local As New HashSet(Of String)()
            For Each m As Match In Regex.Matches(script.Value, "CREATE\s+TABLE\s+" & TableName, RegexOptions.IgnoreCase)
                local.Add(Plain(m.Groups(1).Value))
            Next
            For Each batch As String In Batches(script.Value)
                If Regex.IsMatch(batch, "CREATE\s+(OR\s+ALTER\s+)?(PROCEDURE|FUNCTION|VIEW|TRIGGER)", RegexOptions.IgnoreCase) Then
                    Continue For
                End If
                Dim touched As New HashSet(Of String)()
                For Each pattern As String In patterns
                    For Each m As Match In Regex.Matches(batch, pattern, RegexOptions.IgnoreCase)
                        touched.Add(Plain(m.Groups(1).Value))
                    Next
                Next
                For Each table As String In touched
                    If table.StartsWith("sys.", StringComparison.Ordinal) OrElse table.StartsWith("information_schema.", StringComparison.Ordinal) Then
                        Continue For
                    End If
                    If created.Contains(table) OrElse local.Contains(table) Then
                        Continue For
                    End If
                    ' A script may touch a table that a later script creates only when it first checks that the table is there.
                    Dim guard As String = "(OBJECT_ID|COL_LENGTH)\(N?'" & Regex.Escape(table) & "'[^)]*\)\s+IS\s+NOT\s+NULL"
                    If Regex.IsMatch(batch, guard, RegexOptions.IgnoreCase) Then
                        Continue For
                    End If
                    problems.Add(script.Key & " uses " & table & " before a script creates it")
                Next
            Next
            created.UnionWith(local)
        Next
        Assert.True(problems.Count = 0, String.Join(Environment.NewLine, problems))
    End Sub

    ''' <summary>The last script that defines a procedure decides what a new database gets. These procedures were patched early and defined again later.</summary>
    <Theory>
    <InlineData("mst.usp_EmployeeStatutory_Get", "EpfOnHigherWage")>
    <InlineData("mst.usp_EmployeeStatutory_SaveFlags", "EpfOnHigherWage")>
    <InlineData("mst.usp_EmployeeSeparation_Save", "51008")>
    <InlineData("pay.usp_Policy_Get", "AbsenceDeductionDivisor")>
    <InlineData("pay.usp_Policy_Save", "AbsenceDeductionDivisor")>
    Public Sub TheLastDefinitionOfAPatchedProcedureKeepsThePatch(procedureName As String, token As String)
        Dim lastScript As String = Nothing
        Dim lastBody As String = Nothing
        For Each script As KeyValuePair(Of String, String) In Scripts()
            Dim found As Match = Regex.Match(script.Value, "CREATE\s+OR\s+ALTER\s+PROCEDURE\s+" & Regex.Escape(procedureName) & "\b", RegexOptions.IgnoreCase)
            If found.Success Then
                Dim rest As String = script.Value.Substring(found.Index)
                Dim endMatch As Match = Regex.Match(rest, "^\s*GO\s*$", RegexOptions.Multiline Or RegexOptions.IgnoreCase)
                lastScript = script.Key
                lastBody = If(endMatch.Success, rest.Substring(0, endMatch.Index), rest)
            End If
        Next
        Assert.NotNull(lastScript)
        Assert.True(lastBody.Contains(token), procedureName & " is last defined in " & lastScript & " without " & token & ": an earlier patch is lost on a new database.")
    End Sub
End Class
