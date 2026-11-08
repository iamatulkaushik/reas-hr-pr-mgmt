Imports System.ServiceProcess
Imports System.Threading

''' <summary>
''' The punch agent (Phase 8, ADR 0012 and 0013): a Windows Service on the factory LAN that pulls punches from the devices and sends them to the server through the agt wrapper procedures.
''' With no argument it runs as a service. With run it runs in the console until Ctrl+C, with once it does one round and prints it, and protect writes the settings file.
''' </summary>
Public Module Program
    Private Const Usage As String =
        "RevolutionAgent                      run as a Windows Service" & vbLf &
        "RevolutionAgent run                  run in this window until Ctrl+C" & vbLf &
        "RevolutionAgent once                 do one round, print it, stop" & vbLf &
        "RevolutionAgent protect <connection string> <agent key> [pull seconds]" & vbLf &
        "                                     write agent.settings.json with both values protected for this computer"

    Public Function Main(args As String()) As Integer
        If args.Length = 0 Then
            ServiceBase.Run(New AgentService())
            Return 0
        End If
        Try
            Select Case args(0).ToLowerInvariant()
                Case "protect"
                    Return Protect(args)
                Case "run"
                    Return RunConsole()
                Case "once"
                    Return RunOnce()
                Case Else
                    Console.Error.WriteLine(Usage)
                    Return 2
            End Select
        Catch ex As Exception
            Console.Error.WriteLine("Failed: " & ex.Message)
            Return 1
        End Try
    End Function

    Private Function Protect(args As String()) As Integer
        If args.Length < 3 Then
            Console.Error.WriteLine(Usage)
            Return 2
        End If
        Dim seconds As Integer = AgentSettings.DefaultPullSeconds
        If args.Length > 3 AndAlso Not Integer.TryParse(args(3), seconds) Then
            Console.Error.WriteLine("Pull seconds must be a number.")
            Return 2
        End If
        AgentSettings.Save(AgentSettings.DefaultPath(), args(1), args(2), seconds)
        Console.WriteLine("Saved " & AgentSettings.FileName & ".")
        Return 0
    End Function

    Private Function Build() As AgentLoop
        Dim settings As AgentSettings = AgentSettings.Load(AgentSettings.DefaultPath())
        Return New AgentLoop(settings.PullSeconds, New AgentLog(AgentLog.DefaultPath()), New SqlAgentGateway(settings.ConnectionString, settings.AgentKey), AgentReaders.All())
    End Function

    Private Function RunOnce() As Integer
        Build().RunOnce()
        Console.WriteLine("Done. See " & System.IO.Path.GetFileName(AgentLog.DefaultPath()) & ".")
        Return 0
    End Function

    Private Function RunConsole() As Integer
        Dim runner As AgentLoop = Build()
        Using cancel As New CancellationTokenSource()
            AddHandler Console.CancelKeyPress, Sub(sender As Object, e As ConsoleCancelEventArgs)
                                                   e.Cancel = True
                                                   cancel.Cancel()
                                               End Sub
            runner.RunUntil(cancel.Token)
        End Using
        Return 0
    End Function
End Module
