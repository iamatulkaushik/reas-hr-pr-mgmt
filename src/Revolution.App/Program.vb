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
        If services.Session.Current.Role = UserRole.Associate AndAlso services.Session.Current.CompanyId = 0 Then
            Using picker As New CompanyPickerForm(services.Auth, services.AssociateAdmin)
                If picker.ShowDialog() <> DialogResult.OK Then
                    Return
                End If
            End Using
        End If
        ' Modules register here, one line each, when they are pushed and reviewed (ADR 0016).
        Dim modules As New List(Of IShellModule)()
        Application.Run(New ShellForm(services, modules))
    End Sub

    Private Sub ShowStartupError(message As String)
        MessageBox.Show(message, "Revolution HRMS", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub
End Module
