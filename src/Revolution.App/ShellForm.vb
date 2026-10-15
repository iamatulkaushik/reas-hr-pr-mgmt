Imports System.Drawing
Imports System.Windows.Forms
Imports Revolution.UI.Common

''' <summary>
''' The main window: File menu (switch company for users who work on several, exit) plus the menus the registered modules offer.
''' Screens opened from the menu are closed when the company changes, so no screen ever shows another company's data (ADR 0016).
''' </summary>
Public Class ShellForm
    Inherits BaseForm

    Private ReadOnly _services As AppServices
    Private ReadOnly _modules As IReadOnlyList(Of IShellModule)
    Private ReadOnly _menu As New MenuStrip()
    Private ReadOnly _banner As New Label()
    Private ReadOnly _openScreens As New List(Of Form)()

    Public Sub New(services As AppServices, modules As IEnumerable(Of IShellModule))
        MyBase.New("Revolution HRMS", String.Empty)
        If services Is Nothing Then Throw New ArgumentNullException(NameOf(services))
        If modules Is Nothing Then Throw New ArgumentNullException(NameOf(modules))
        _services = services
        _modules = modules.ToList()
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

    Private Sub RebuildMenu()
        _menu.Items.Clear()
        _menu.Items.Add(BuildFileMenu())
        For Each groupMenu In ShellMenuBuilder.Build(_modules, _services.Permissions, _services.Entitlements, AddressOf OnOpenItem)
            _menu.Items.Add(groupMenu)
        Next
        Dim user = _services.Session.Current
        _banner.Text = If(user Is Nothing, "Not signed in",
                          $"Signed in as {user.UserName} ({user.Role}, {user.Plan}), company {user.CompanyId}")
    End Sub

    Private Function BuildFileMenu() As ToolStripMenuItem
        Dim fileMenu As New ToolStripMenuItem("&File")
        If CanSwitchCompany() Then
            Dim switchItem As New ToolStripMenuItem("&Switch company...")
            AddHandler switchItem.Click, AddressOf OnSwitchCompany
            fileMenu.DropDownItems.Add(switchItem)
        End If
        Dim exitItem As New ToolStripMenuItem("E&xit")
        AddHandler exitItem.Click, Sub(sender As Object, args As EventArgs) Close()
        fileMenu.DropDownItems.Add(exitItem)
        Return fileMenu
    End Function

    ''' <summary>True when the signed-in user works on more than one company. The auth service decides; the shell never checks a role name.</summary>
    Private Function CanSwitchCompany() As Boolean
        Dim companies = _services.Auth.ListCompanies()
        Return companies.Succeeded AndAlso companies.Value.Count > 1
    End Function

    Private Sub OnOpenItem(item As ShellItem)
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

    Private Sub OnSwitchCompany(sender As Object, args As EventArgs)
        If _openScreens.Count > 0 Then
            Dim answer = MessageBox.Show(Me, "Switching company closes the open screens. Unsaved work is lost. Continue?", Text,
                                         MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            If answer <> DialogResult.Yes Then
                Return
            End If
        End If
        For Each openForm In _openScreens.ToList()
            openForm.Close()
        Next
        If _openScreens.Count > 0 Then
            ShowError("A screen did not close, so the company was not changed. Finish or cancel its work and try again.")
            Return
        End If
        Using picker As New CompanyPickerForm(_services.Auth, _services.AssociateAdmin)
            If picker.ShowDialog(Me) = DialogResult.OK Then
                RebuildMenu()
            End If
        End Using
    End Sub
End Class
