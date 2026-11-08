Imports System.ServiceProcess
Imports System.Threading
Imports System.Threading.Tasks

''' <summary>The Windows Service wrapper around AgentLoop. Start and stop come from the Services console or sc.exe. It reads agent.settings.json when it starts.</summary>
Public NotInheritable Class AgentService
    Inherits ServiceBase

    Private _cancel As CancellationTokenSource
    Private _worker As Task

    Public Sub New()
        ServiceName = "RevolutionAgent"
        CanStop = True
    End Sub

    Protected Overrides Sub OnStart(args As String())
        Dim log As New AgentLog(AgentLog.DefaultPath())
        Dim settings As AgentSettings
        Try
            settings = AgentSettings.Load(AgentSettings.DefaultPath())
        Catch ex As Exception
            log.Write("The settings could not be read: " & ex.GetType().Name & ".")
            Throw
        End Try
        _cancel = New CancellationTokenSource()
        Dim loopRunner As New AgentLoop(settings.PullSeconds, log, New SqlAgentGateway(settings.ConnectionString, settings.AgentKey), AgentReaders.All())
        Dim token As CancellationToken = _cancel.Token
        _worker = Task.Run(Sub() loopRunner.RunUntil(token))
    End Sub

    Protected Overrides Sub OnStop()
        If _cancel IsNot Nothing Then _cancel.Cancel()
        If _worker IsNot Nothing Then _worker.Wait(TimeSpan.FromSeconds(20))
    End Sub
End Class
