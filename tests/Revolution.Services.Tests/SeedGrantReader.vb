Imports System.IO
Imports System.Text.RegularExpressions

''' <summary>
''' Reads the role grants the migration scripts insert, so the test checks the real seed and not a copy of it.
''' Understands the two forms used so far: the 0004 spec rows (role, module, 'action,action') and plain (role, module, action) rows.
''' </summary>
Friend NotInheritable Class SeedGrantReader
    Private Shared ReadOnly SpecRow As New Regex("\(\s*(\d+)\s*,\s*(\d+)\s*,\s*'([\d,\s]+)'\s*\)", RegexOptions.Compiled)
    Private Shared ReadOnly PlainRow As New Regex("\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)", RegexOptions.Compiled)
    Private Shared ReadOnly Removal As New Regex("\b(DELETE|TRUNCATE)\b[^\r\n;]*sec\.RolePermission", RegexOptions.Compiled Or RegexOptions.IgnoreCase)

    Private Sub New()
    End Sub

    ''' <summary>Every "role/module/action" the scripts that insert into sec.RolePermission grant.</summary>
    Friend Shared Function ReadGrants() As HashSet(Of String)
        Dim grants As New HashSet(Of String)()
        For Each script In InsertingScripts()
            For Each row As Match In SpecRow.Matches(script)
                For Each action In row.Groups(3).Value.Split(","c)
                    grants.Add(MatrixSpec.KeyOf(CInt(row.Groups(1).Value), CInt(row.Groups(2).Value), CInt(action.Trim())))
                Next
            Next
            For Each row As Match In PlainRow.Matches(script)
                grants.Add(MatrixSpec.KeyOf(CInt(row.Groups(1).Value), CInt(row.Groups(2).Value), CInt(row.Groups(3).Value)))
            Next
        Next
        Return grants
    End Function

    ''' <summary>Names of scripts that remove grants. The reader only adds, so such a script means the reader must learn about it.</summary>
    Friend Shared Function ScriptsThatRemoveGrants() As List(Of String)
        Return Directory.GetFiles(MigrationsFolder(), "*.sql").
            Where(Function(path) Removal.IsMatch(File.ReadAllText(path))).
            Select(Function(path) System.IO.Path.GetFileName(path)).
            OrderBy(Function(name) name, StringComparer.Ordinal).ToList()
    End Function

    Private Shared Function InsertingScripts() As List(Of String)
        Return Directory.GetFiles(MigrationsFolder(), "*.sql").
            OrderBy(Function(path) path, StringComparer.Ordinal).
            Select(Function(path) File.ReadAllText(path)).
            Where(Function(script) script.Contains("INSERT sec.RolePermission", StringComparison.OrdinalIgnoreCase)).
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
