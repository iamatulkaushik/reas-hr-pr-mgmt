Imports System.Drawing
Imports System.Windows.Forms
Imports Revolution.UI.Common

''' <summary>
''' The main window: File menu (select company, financial year, sign out, exit) plus the menus the registered modules offer.
''' Screens opened from the menu are closed when the company changes or the user signs out, so no screen ever shows
''' another company's data (ADR 0016).
''' </summary>
Public Class ShellForm
    Inherits BaseForm

    Private ReadOnly _services As AppServices
    Private ReadOnly _modules As IReadOnlyList(Of IShellModule)
    Private ReadOnly _serverInfo As String
    Private ReadOnly _menu As New MenuStrip()
    Private ReadOnly _banner As New Label()
    Private ReadOnly _openScreens As New List(Of Form)()
    Private _signOutRequested As Boolean

    ''' <param name="serverInfo">Server and database name shown in the status bar. Never contains a password.</param>
    Public Sub New(services As AppServices, modules As IEnumerable(Of IShellModule), serverInfo As String)
        MyBase.New("Revolution HRMS", String.Empty)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        If modules Is Nothing Then Throw New ArgumentNullException(NameOf(modules))
        _services = services
        _modules = modules.ToList()
        _serverInfo = If(serverInfo, String.Empty)
        StartPosition = FormStartPosition.CenterScreen
        ClientSize = New Size(1100, 700)
        _banner.Dock = DockStyle.Fill
        _banner.TextAlign = ContentAlignment.MiddleCenter
        _menu.Dock = DockStyle.Top
        ' Fill control first, then the menu, so the menu docks to the top.
        Content.Controls.Add(_banner)
        Content.Controls.Add(_menu)
        MainMenuStrip = _menu
        RebuildMenu()
    End Sub

    ''' <summary>True when the window was closed by Sign Out, so the caller shows the login again instead of ending the program.</summary>
    Public ReadOnly Property SignOutRequested As Boolean
        Get
            Return _signOutRequested
        End Get
    End Property

    Private Sub RebuildMenu()
        _menu.Items.Clear()
        _menu.Items.Add(BuildFileMenu())
        For Each groupMenu In ShellMenuBuilder.Build(_modules, _services.Permissions, _services.Entitlements, AddressOf OnOpenItem, AddressOf MenuIconMap.IconFor, DeviceDpi)
            _menu.Items.Add(groupMenu)
        Next
        Dim user = _services.Session.Current
        _banner.Text = If(user Is Nothing, "Not signed in",
                          $"Signed in as {user.UserName} ({user.Role}, {user.Plan}), company {user.CompanyId}")
        UpdateStatusBar()
        ShowLicenceNotice()
    End Sub

    ''' <summary>Right of the status bar: current financial year, server and database, the signed-in user and the company.</summary>
    Private Sub UpdateStatusBar()
        Dim user = _services.Session.Current
        If user Is Nothing Then
            SetStatusInfo("Not signed in")
            Return
        End If
        Dim panels As New List(Of String) From {
            "FY " & FinancialYearCalendar.Label(FinancialYearCalendar.StartYear(Date.Today)),
            "Server: " & _serverInfo,
            $"{If(user.Role = UserRole.Associate, "Associate", user.Role.ToString())}: {user.UserName} (id {user.UserId})"}
        If user.CompanyId > 0 Then
            panels.Add("Company: " & LookUpCompanyName(user.CompanyId))
        End If
        SetStatusInfo(panels.ToArray())
    End Sub

    Private Function LookUpCompanyName(companyId As Integer) As String
        Dim companies = _services.Auth.ListCompanies()
        If Not companies.Succeeded Then
            Return "#" & companyId.ToString(Globalization.CultureInfo.InvariantCulture)
        End If
        Dim match = companies.Value.FirstOrDefault(Function(c) c.CompanyId = companyId)
        Return If(match Is Nothing, "#" & companyId.ToString(Globalization.CultureInfo.InvariantCulture), match.CompanyName)
    End Function

    ''' <summary>Shows the licence or trial line when there is one. Grace and read-only are shown as a warning.</summary>
    Private Sub ShowLicenceNotice()
        Dim licence = _services.Licence.Status
        If licence.Banner.Length = 0 Then
            Return
        End If
        If licence.IsReadOnly OrElse licence.State = LicenceState.Grace Then
            ShowError(licence.Banner)
        Else
            ShowStatus(licence.Banner)
        End If
    End Sub

    Private Function BuildFileMenu() As ToolStripMenuItem
        Dim fileMenu As New ToolStripMenuItem("&File")
        Dim selectItem As New ToolStripMenuItem("Select &Company...") With {.Enabled = CanSwitchCompany(), .Image = ShellMenuBuilder.MenuImage(AppIcon.CompanySwitch, DeviceDpi)}
        AddHandler selectItem.Click, AddressOf OnSwitchCompany
        Dim yearItem As New ToolStripMenuItem("&Financial Year...") With {.Enabled = False, .ToolTipText = "Planned: not available yet.", .Image = ShellMenuBuilder.MenuImage(AppIcon.Calendar, DeviceDpi)}
        Dim signOutItem As New ToolStripMenuItem("&Sign Out") With {.Image = ShellMenuBuilder.MenuImage(AppIcon.Logout, DeviceDpi)}
        AddHandler signOutItem.Click, AddressOf OnSignOut
        Dim exitItem As New ToolStripMenuItem("E&xit") With {.Image = ShellMenuBuilder.MenuImage(AppIcon.[Exit], DeviceDpi)}
        AddHandler exitItem.Click, Sub(sender As Object, args As EventArgs) Close()
        fileMenu.DropDownItems.AddRange(New ToolStripItem() {selectItem, yearItem, signOutItem, New ToolStripSeparator(), exitItem})
        Return fileMenu
    End Function

    ''' <summary>True when the signed-in user works on more than one company. The auth service decides; the shell never checks a role name.</summary>
    Private Function CanSwitchCompany() As Boolean
        Dim companies = _services.Auth.ListCompanies()
        Return companies.Succeeded AndAlso companies.Value.Count > 1
    End Function

    Private Sub OnOpenItem(item As ShellItem)
        If item.IsPlanned Then
            ShowStatus("This screen is planned and not available yet.")
            Return
        End If
        If Not _services.Permissions.IsAllowed(item.Entry.ModuleKey, PermissionAction.View) Then
            ShowError("You do not have permission to open this screen.")
            Return
        End If
        ClearValidation()
        Dim screen As Form = item.OpenScreen.Invoke()
        _openScreens.Add(screen)
        AddHandler screen.FormClosed, Sub(sender As Object, args As FormClosedEventArgs) _openScreens.Remove(screen)
        screen.Show(Me)
    End Sub

    ''' <summary>Closes every open screen. False when one refused to close (unsaved work), so the caller must not go on.</summary>
    Private Function CloseOpenScreens() As Boolean
        For Each openForm In _openScreens.ToList()
            openForm.Close()
        Next
        Return _openScreens.Count = 0
    End Function

    Private Sub OnSwitchCompany(sender As Object, args As EventArgs)
        If _openScreens.Count > 0 Then
            Dim answer = MessageBox.Show(Me, "Selecting another company closes the open screens. Unsaved work is lost. Continue?", Text,
                                         MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            If answer <> DialogResult.Yes Then
                Return
            End If
        End If
        If Not CloseOpenScreens() Then
            ShowError("A screen did not close, so the company was not changed. Finish or cancel its work and try again.")
            Return
        End If
        Using picker As New CompanyPickerForm(_services.Auth, _services.AssociateAdmin)
            If picker.ShowDialog(Me) = DialogResult.OK Then
                RebuildMenu()
            End If
        End Using
    End Sub

    Private Sub OnSignOut(sender As Object, args As EventArgs)
        Dim answer = MessageBox.Show(Me, "Sign out? Open screens are closed and unsaved work is lost.", Text,
                                     MessageBoxButtons.YesNo, MessageBoxIcon.Question)
        If answer <> DialogResult.Yes Then
            Return
        End If
        If Not CloseOpenScreens() Then
            ShowError("A screen did not close, so you were not signed out. Finish or cancel its work and try again.")
            Return
        End If
        _signOutRequested = True
        Close()
    End Sub
End Class
