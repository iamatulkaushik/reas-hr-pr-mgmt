Imports System.Windows.Forms
Imports Revolution.Data
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
            MessageBox.Show("appsettings.json is missing or has no ConnectionStrings:Hrms setting. Copy appsettings.example.json and edit it.",
                            "Revolution HRMS", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        Dim session As New SessionContext()
        Dim factory As New SqlConnectionFactory(connection, session)
        Dim entitlements As New EntitlementService(session)
        Dim audit As New AuditService(New SqlAuditRepository(factory), session, entitlements)
        Dim auth As New AuthService(New SqlUserRepository(factory), New SqlCompanyRepository(factory),
                                    New PasswordHasher(), session, audit, TimeProvider.System)

        Using login As New LoginForm(auth, session)
            If login.ShowDialog() <> DialogResult.OK Then
                Return
            End If
        End Using
        Application.Run(New ShellForm(session))
    End Sub
End Module
