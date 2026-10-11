Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions
Imports Microsoft.Data.SqlClient

''' <summary>
''' Applies database/migrations in number order and records each in cfg.SchemaVersion with a checksum.
''' A script edited after it was applied stops the run (ADR 0004: never edit a merged script).
''' This is the one place that runs SQL text instead of a stored procedure, because it creates the procedures.
''' </summary>
Public NotInheritable Class MigrationRunner
    Private ReadOnly _connectionString As String

    Public Sub New(connectionString As String)
        _connectionString = connectionString
    End Sub

    ''' <summary>Returns how many scripts were applied.</summary>
    Public Function Run(folder As String) As Integer
        Dim scripts = Directory.GetFiles(folder, "*.sql").OrderBy(Function(f) Path.GetFileName(f), StringComparer.Ordinal).ToList()
        Dim applied = 0
        Using conn As New SqlConnection(_connectionString)
            conn.Open()
            For Each scriptFile In scripts
                If ApplyIfNew(conn, scriptFile) Then
                    applied += 1
                End If
            Next
        End Using
        Return applied
    End Function

    Private Shared Function ApplyIfNew(conn As SqlConnection, scriptPath As String) As Boolean
        Dim fileName = Path.GetFileName(scriptPath)
        Dim number = fileName.Substring(0, 4)
        Dim text = File.ReadAllText(scriptPath).Replace(vbCrLf, vbLf)
        Dim checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant()
        Dim recorded = FindChecksum(conn, number)
        If recorded IsNot Nothing Then
            If Not String.Equals(recorded, checksum, StringComparison.OrdinalIgnoreCase) Then
                Throw New InvalidOperationException($"{fileName} was edited after it was applied. Add a new numbered script instead.")
            End If
            Return False
        End If
        Console.WriteLine($"Applying {fileName}")
        For Each batch In SplitBatches(text)
            Using cmd = conn.CreateCommand()
                cmd.CommandText = batch
                cmd.ExecuteNonQuery()
            End Using
        Next
        Record(conn, number, fileName, checksum)
        Return True
    End Function

    Private Shared Function FindChecksum(conn As SqlConnection, number As String) As String
        Using cmd = conn.CreateCommand()
            cmd.CommandText = "IF OBJECT_ID(N'cfg.SchemaVersion', N'U') IS NULL SELECT NULL " &
                              "ELSE SELECT Checksum FROM cfg.SchemaVersion WHERE ScriptNumber = @n"
            cmd.Parameters.AddWithValue("@n", number)
            Dim raw = cmd.ExecuteScalar()
            Return If(raw Is Nothing OrElse raw Is DBNull.Value, Nothing, Convert.ToString(raw).Trim())
        End Using
    End Function

    Private Shared Sub Record(conn As SqlConnection, number As String, fileName As String, checksum As String)
        Using cmd = conn.CreateCommand()
            cmd.CommandText = "INSERT cfg.SchemaVersion (ScriptNumber, ScriptName, Checksum) VALUES (@n, @name, @c)"
            cmd.Parameters.AddWithValue("@n", number)
            cmd.Parameters.AddWithValue("@name", fileName)
            cmd.Parameters.AddWithValue("@c", checksum)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>Splits on lines that contain only GO, like sqlcmd does.</summary>
    Private Shared Function SplitBatches(text As String) As IEnumerable(Of String)
        Return Regex.Split(text, "^[ \t]*GO[ \t]*$", RegexOptions.Multiline Or RegexOptions.IgnoreCase) _
                    .Where(Function(b) Not String.IsNullOrWhiteSpace(b))
    End Function
End Class
