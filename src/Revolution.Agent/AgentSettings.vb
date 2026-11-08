Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.Json

''' <summary>
''' The agent's own settings, kept next to the program in agent.settings.json. The connection string of the agent login (hrms_agent, created by the Setup tool) and the agent key are stored
''' protected for this computer (Windows data protection, machine scope), never in plain text, and are never written to a log. PullSeconds is how long the agent waits between rounds (15 to 3600).
''' </summary>
Public NotInheritable Class AgentSettings
    Public Const FileName As String = "agent.settings.json"
    Public Const DefaultPullSeconds As Integer = 60
    Public Const MinPullSeconds As Integer = 15
    Public Const MaxPullSeconds As Integer = 3600

    Private Shared ReadOnly Entropy As Byte() = Encoding.UTF8.GetBytes("RevolutionAgent.v1")

    Public Sub New(connectionString As String, agentKey As String, pullSeconds As Integer)
        Me.ConnectionString = connectionString
        Me.AgentKey = agentKey
        Me.PullSeconds = Math.Min(MaxPullSeconds, Math.Max(MinPullSeconds, pullSeconds))
    End Sub

    Public ReadOnly Property ConnectionString As String
    Public ReadOnly Property AgentKey As String
    Public ReadOnly Property PullSeconds As Integer

    Public Shared Function DefaultPath() As String
        Return Path.Combine(AppContext.BaseDirectory, FileName)
    End Function

    ''' <summary>Writes the settings file with the connection string and the key protected for this computer.</summary>
    Public Shared Sub Save(path As String, connectionString As String, agentKey As String, pullSeconds As Integer)
        If String.IsNullOrWhiteSpace(connectionString) Then Throw New ArgumentException("Give the connection string.", NameOf(connectionString))
        If String.IsNullOrWhiteSpace(agentKey) Then Throw New ArgumentException("Give the agent key.", NameOf(agentKey))
        Dim file As New Dictionary(Of String, Object) From {
            {"connection", Protect(connectionString)}, {"key", Protect(agentKey)}, {"pullSeconds", Math.Min(MaxPullSeconds, Math.Max(MinPullSeconds, pullSeconds))}}
        System.IO.File.WriteAllText(path, JsonSerializer.Serialize(file, New JsonSerializerOptions With {.WriteIndented = True}), New UTF8Encoding(False))
    End Sub

    Public Shared Function Load(path As String) As AgentSettings
        If Not System.IO.File.Exists(path) Then Throw New FileNotFoundException("The agent settings file is missing. Run: RevolutionAgent protect <connection string> <agent key>")
        Using document As JsonDocument = JsonDocument.Parse(System.IO.File.ReadAllText(path, Encoding.UTF8))
            Dim root As JsonElement = document.RootElement
            Dim seconds As Integer = DefaultPullSeconds
            Dim element As JsonElement
            If root.TryGetProperty("pullSeconds", element) AndAlso element.ValueKind = JsonValueKind.Number Then seconds = element.GetInt32()
            Return New AgentSettings(Unprotect(root.GetProperty("connection").GetString()), Unprotect(root.GetProperty("key").GetString()), seconds)
        End Using
    End Function

    Private Shared Function Protect(text As String) As String
        Return Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(text), Entropy, DataProtectionScope.LocalMachine))
    End Function

    Private Shared Function Unprotect(text As String) As String
        Return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(text), Entropy, DataProtectionScope.LocalMachine))
    End Function
End Class
