Imports System.Threading
Imports Revolution.Modules.Biometric

''' <summary>Runs pull rounds until stopped: one round, a wait of PullSeconds, again. A round that fails, or throws something unexpected, is logged and the loop goes on, because an agent that dies on one bad round loses a night of punches.</summary>
Public NotInheritable Class AgentLoop
    Private ReadOnly _pullSeconds As Integer
    Private ReadOnly _log As AgentLog
    Private ReadOnly _gateway As IAgentGateway
    Private ReadOnly _readers As IReadOnlyList(Of IPunchSource)

    Public Sub New(pullSeconds As Integer, log As AgentLog, gateway As IAgentGateway, readers As IReadOnlyList(Of IPunchSource))
        _pullSeconds = pullSeconds
        _log = log
        _gateway = gateway
        _readers = readers
    End Sub

    Public Sub RunOnce()
        Try
            For Each line As String In AgentRound.Run(_gateway, _readers, DateTime.Now).Lines
                _log.Write(line)
            Next
        Catch ex As Exception
            _log.Write("Round failed: " & ex.GetType().Name & ".")
        End Try
    End Sub

    Public Sub RunUntil(token As CancellationToken)
        _log.Write("Agent started.")
        While Not token.IsCancellationRequested
            RunOnce()
            token.WaitHandle.WaitOne(TimeSpan.FromSeconds(_pullSeconds))
        End While
        _log.Write("Agent stopped.")
    End Sub
End Class
