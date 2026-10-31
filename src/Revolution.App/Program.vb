Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient
Imports Revolution.Modules.Attendance
Imports Revolution.Modules.Attendance.UI
Imports Revolution.Modules.Employees
Imports Revolution.Modules.Employees.UI
Imports Revolution.Modules.Payroll
Imports Revolution.Modules.Payroll.UI
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
        Dim modules = BuildModules(services)
        Dim serverInfo = DescribeServer(connection)

        Dim signedOut As Boolean
        Do
            If Not SignInAndSelectCompany(services) Then
                Return
            End If
            Using shell As New ShellForm(services, modules, serverInfo)
                Application.Run(shell)
                signedOut = shell.SignOutRequested
            End Using
            If signedOut Then
                services.Session.SignOut()
            End If
        Loop While signedOut
    End Sub

    ''' <summary>
    ''' Feature modules are composed here and offer their screens to MenuLayoutModule, which places them in the menu.
    ''' A new module is added to the sources list (ADR 0016); its menu positions are set in MenuLayoutModule.
    ''' </summary>
    Private Function BuildModules(services As AppServices) As List(Of IShellModule)
        Dim employees = EmployeesModule.Compose(services.Host)
        Dim sources As New List(Of IShellModule) From {
            New EmployeesShellModule(employees, services.Permissions),
            New AttendanceShellModule(AttendanceModule.Compose(services.Host), services.Permissions),
            New PayrollShellModule(PayrollModule.Compose(services.Host, employees.Export), services.Permissions)}
        Dim layout As New MenuLayoutModule(sources)
        Return New List(Of IShellModule) From {
            layout,
            New LicenceShellModule(services.LicenceAdmin),
            New IconMapShellModule(Function() layout.Items, Function() sources.SelectMany(Function(m) m.Items).ToList(), services.Permissions)}
    End Function

    ''' <summary>Login, then the company picker for a user who works on several companies. False when the user cancels.</summary>
    Private Function SignInAndSelectCompany(services As AppServices) As Boolean
        Using login As New LoginForm(services.Auth, services.Session)
            If login.ShowDialog() <> DialogResult.OK Then
                Return False
            End If
        End Using
        If services.Session.Current.Role = UserRole.Associate AndAlso services.Session.Current.CompanyId = 0 Then
            Using picker As New CompanyPickerForm(services.Auth, services.AssociateAdmin)
                Return picker.ShowDialog() = DialogResult.OK
            End Using
        End If
        Return True
    End Function

    ''' <summary>Server and database name for the status bar. Only these two parts of the connection string are shown, never the password.</summary>
    Private Function DescribeServer(connection As String) As String
        Try
            Dim builder As New SqlConnectionStringBuilder(connection)
            Return $"{builder.DataSource} / {builder.InitialCatalog}"
        Catch ex As ArgumentException
            Return "unknown"
        End Try
    End Function

    Private Sub ShowStartupError(message As String)
        MessageBox.Show(message, "Revolution HRMS", MessageBoxButtons.OK, MessageBoxIcon.Error)
    End Sub
End Module
