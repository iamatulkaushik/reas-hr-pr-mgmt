Imports System.IO
Imports System.Text.RegularExpressions
Imports Revolution.Core

''' <summary>
''' Reads the role grants the migration scripts insert, so the test checks the real seed and not a copy of it.
''' Understands the forms used so far: the 0004 spec rows (role, module, 'action,action'), plain (role, module, action) rows, and the one-row form
''' INSERT ... SELECT role, module, action WHERE NOT EXISTS (0032). It also applies a script's DELETE statements (0034) before that script's inserts,
''' because a script that narrows a seed removes first and then asserts the rows that stay.
''' </summary>
Friend NotInheritable Class SeedGrantReader
    Private Shared ReadOnly SpecRow As New Regex("\(\s*(\d+)\s*,\s*(\d+)\s*,\s*'([\d,\s]+)'\s*\)", RegexOptions.Compiled)
    Private Shared ReadOnly PlainRow As New Regex("\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)", RegexOptions.Compiled)
    Private Shared ReadOnly SelectRow As New Regex("\bSELECT\s+(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s+WHERE\s+NOT\s+EXISTS", RegexOptions.Compiled Or RegexOptions.IgnoreCase)
    Private Shared ReadOnly DeleteStatement As New Regex("\bDELETE\s+FROM\s+sec\.RolePermission\s+WHERE\s+([^;]+);", RegexOptions.Compiled Or RegexOptions.IgnoreCase)
    Private Shared ReadOnly RoleTest As New Regex("RoleID\s*(?:=\s*(\d+)|IN\s*\(([\d,\s]+)\))", RegexOptions.Compiled Or RegexOptions.IgnoreCase)
    Private Shared ReadOnly ModuleTest As New Regex("ModuleID\s*(?:=\s*(\d+)|IN\s*\(([\d,\s]+)\))", RegexOptions.Compiled Or RegexOptions.IgnoreCase)
    Private Shared ReadOnly ActionTest As New Regex("ActionID\s*(?:=\s*(\d+)|(NOT\s+)?IN\s*\(([\d,\s]+)\))", RegexOptions.Compiled Or RegexOptions.IgnoreCase)
    Private Shared ReadOnly Removal As New Regex("\b(DELETE|TRUNCATE)\b[^\r\n;]*sec\.RolePermission", RegexOptions.Compiled Or RegexOptions.IgnoreCase)

    Private Sub New()
    End Sub

    ''' <summary>Every "role/module/action" the scripts leave granted: each script's deletes are applied first, then its inserts, scripts in file-name order.</summary>
    Friend Shared Function ReadGrants() As HashSet(Of String)
        Dim grants As New HashSet(Of String)()
        For Each script In ChangingScripts()
            For Each statement As Match In DeleteStatement.Matches(script)
                ApplyDelete(grants, statement.Groups(1).Value)
            Next
            For Each row As Match In SpecRow.Matches(script)
                For Each action In row.Groups(3).Value.Split(","c)
                    grants.Add(MatrixSpec.KeyOf(CInt(row.Groups(1).Value), CInt(row.Groups(2).Value), CInt(action.Trim())))
                Next
            Next
            For Each row As Match In PlainRow.Matches(script)
                grants.Add(MatrixSpec.KeyOf(CInt(row.Groups(1).Value), CInt(row.Groups(2).Value), CInt(row.Groups(3).Value)))
            Next
            For Each row As Match In SelectRow.Matches(script)
                grants.Add(MatrixSpec.KeyOf(CInt(row.Groups(1).Value), CInt(row.Groups(2).Value), CInt(row.Groups(3).Value)))
            Next
        Next
        Return grants
    End Function

    Private Shared Sub ApplyDelete(grants As HashSet(Of String), condition As String)
        Dim roles = Numbers(RoleTest.Match(condition))
        Dim modules = Numbers(ModuleTest.Match(condition))
        Dim actionMatch = ActionTest.Match(condition)
        For Each role In roles
            For Each moduleId In modules
                For Each value In [Enum].GetValues(GetType(PermissionAction))
                    Dim action = CInt(value)
                    If ActionSelected(actionMatch, action) Then grants.Remove(MatrixSpec.KeyOf(role, moduleId, action))
                Next
            Next
        Next
    End Sub

    Private Shared Function ActionSelected(match As Match, action As Integer) As Boolean
        If Not match.Success Then Return True
        If match.Groups(1).Success Then Return CInt(match.Groups(1).Value) = action
        Dim listed = match.Groups(3).Value.Split(","c).Select(Function(item) CInt(item.Trim())).Contains(action)
        Return If(match.Groups(2).Success, Not listed, listed)
    End Function

    Private Shared Function Numbers(match As Match) As List(Of Integer)
        If match.Groups(1).Success Then Return New List(Of Integer) From {CInt(match.Groups(1).Value)}
        Return match.Groups(2).Value.Split(","c).Select(Function(item) CInt(item.Trim())).ToList()
    End Function

    ''' <summary>Names of scripts with a removal the reader cannot apply (a TRUNCATE, or a DELETE whose role and module it cannot read). The reader applies the rest.</summary>
    Friend Shared Function ScriptsThatRemoveGrants() As List(Of String)
        Return Directory.GetFiles(MigrationsFolder(), "*.sql").
            Where(Function(path) HasUnreadRemoval(File.ReadAllText(path))).
            Select(Function(path) System.IO.Path.GetFileName(path)).
            OrderBy(Function(name) name, StringComparer.Ordinal).ToList()
    End Function

    Private Shared Function HasUnreadRemoval(script As String) As Boolean
        Dim removals = Removal.Matches(script).Count
        If removals = 0 Then Return False
        Dim readable = DeleteStatement.Matches(script).Cast(Of Match)().
            Count(Function(m) RoleTest.IsMatch(m.Groups(1).Value) AndAlso ModuleTest.IsMatch(m.Groups(1).Value))
        Return readable <> removals
    End Function

    Private Shared Function ChangingScripts() As List(Of String)
        Return Directory.GetFiles(MigrationsFolder(), "*.sql").
            OrderBy(Function(path) path, StringComparer.Ordinal).
            Select(Function(path) File.ReadAllText(path)).
            Where(Function(script) script.Contains("INSERT sec.RolePermission", StringComparison.OrdinalIgnoreCase) OrElse DeleteStatement.IsMatch(script)).
            ToList()
    End Function

    Private Shared Function MigrationsFolder() As String
        Dim folder = New DirectoryInfo(AppContext.BaseDirectory)
        Do While folder IsNot Nothing
            If File.Exists(System.IO.Path.Combine(folder.FullName, "Revolution.slnx")) Then
                Return System.IO.Path.Combine(folder.FullName, "database", "migrations")
            End If
            folder = folder.Parent
        Loop
        Throw New InvalidOperationException("Revolution.slnx was not found above the test folder, so the migration scripts cannot be read.")
    End Function
End Class
