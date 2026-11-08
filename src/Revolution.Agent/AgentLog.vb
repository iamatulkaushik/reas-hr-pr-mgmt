Imports System.Globalization
Imports System.IO
Imports System.Text

''' <summary>A plain log next to the program, agent.log. Lines carry a time and the text the agent was given (device codes and counts only). It never gets a key, a connection string, a host or a punch. At about 2 MB it moves to agent.log.old.</summary>
Public NotInheritable Class AgentLog
    Private Const MaxBytes As Long = 2L * 1024L * 1024L

    Private ReadOnly _path As String
    Private ReadOnly _gate As New Object()

    Public Sub New(path As String)
        _path = path
    End Sub

    Public Shared Function DefaultPath() As String
        Return System.IO.Path.Combine(AppContext.BaseDirectory, "agent.log")
    End Function

    Public Sub Write(line As String)
        SyncLock _gate
            Try
                If System.IO.File.Exists(_path) AndAlso New FileInfo(_path).Length > MaxBytes Then
                    Dim old As String = _path & ".old"
                    If System.IO.File.Exists(old) Then System.IO.File.Delete(old)
                    System.IO.File.Move(_path, old)
                End If
                System.IO.File.AppendAllText(_path, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) & " " & line & Environment.NewLine, Encoding.UTF8)
            Catch ex As IOException
                ' A log that cannot be written must not stop the agent.
            Catch ex As UnauthorizedAccessException
                ' Same.
            End Try
        End SyncLock
    End Sub
End Class
