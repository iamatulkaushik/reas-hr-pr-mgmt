Imports System.Windows.Forms
Imports Revolution.Services
Imports Revolution.UI.Common

Public Module Program
    <STAThread>
    Public Sub Main()
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)

        Dim connection = AppSettings.LoadConnectionString()
        If String.IsNullOrWhiteSpace(connection) Then
            ShowStartupError("appsettings.json is missing or has no ConnectionStrings:Hrms setting. Copy appsettings.example.json and edit it.")
            Return
        End If

        Dim built = AppServices.Build(connection, DpapiFieldKeyStore.DefaultPath())
        If Not built.Succeeded Then
            ShowStartupError(built.Message)
            Return
        End If
        Dim services = built.Value

        Using login As New LoginForm(services.Auth, services.Session)
            If login.ShowDialog() <> DialogResult.OK Then
                Return
            End If
        End Using
        Application.Run(New ShellForm(services.Session))
    End Sub

    Private Sub ShowStartupError(message As String)
        MessageBox.Show(message, "Revolution HRMS", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub
End Module
