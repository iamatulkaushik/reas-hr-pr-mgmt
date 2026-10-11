Imports System.IO
Imports System.Text.Json

''' <summary>Reads the database connection string from appsettings.json next to the exe (the file is git-ignored).</summary>
Public NotInheritable Class AppSettings
    Private Sub New()
    End Sub

    ''' <summary>Returns Nothing when the file or the setting is missing or unreadable.</summary>
    Public Shared Function LoadConnectionString() As String
        Dim path = IO.Path.Combine(AppContext.BaseDirectory, "appsettings.json")
        If Not File.Exists(path) Then
            Return Nothing
        End If
        Try
            Using doc = JsonDocument.Parse(File.ReadAllText(path))
                Dim section As JsonElement
                Dim value As JsonElement
                If doc.RootElement.TryGetProperty("ConnectionStrings", section) AndAlso section.TryGetProperty("Hrms", value) Then
                    Return value.GetString()
                End If
            End Using
        Catch ex As JsonException
            Return Nothing
        End Try
        Return Nothing
    End Function
End Class
