Imports System.Drawing
Imports System.Windows.Forms
Imports Revolution.Core

''' <summary>
''' The company settings screen: a list of settings on the left and the chosen setting's own screen on the right.
''' The list shows only the settings the signed-in user may open (same permission and plan check as the menu). Each setting saves on its own,
''' so this form has no Save of its own; Ctrl+S goes to the setting that is open. Closing a setting's screen leaves the list free to open it again.
''' </summary>
Public NotInheritable Class CompanySettingsForm
    Inherits BaseForm

    Private Const ListWidth As Integer = 220
    Private Const FormWidth As Integer = 1100
    Private Const FormHeight As Integer = 720

    Private ReadOnly _list As New ListBox()
    Private ReadOnly _host As New Panel()
    Private _current As Form

    Public Sub New(sections As IEnumerable(Of SettingsSection), permissions As IPermissionService, entitlements As IEntitlementService)
        MyBase.New("Company settings", "Choose a setting on the left. Each setting saves on its own.")
        If sections Is Nothing Then Throw New ArgumentNullException(NameOf(sections))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        If entitlements Is Nothing Then Throw New ArgumentNullException(NameOf(entitlements))
        ClientSize = New Size(FormWidth, FormHeight)
        Dim all As List(Of SettingsSection) = sections.ToList()
        Dim allowed As IReadOnlyList(Of MenuEntry) = MenuFilter.Visible(all.Select(Function(section) section.Entry), permissions, entitlements)
        Dim visible As List(Of SettingsSection) = allowed.Select(Function(entry) all.First(Function(section) section.Entry Is entry)).ToList()
        BuildLayout(visible)
    End Sub

    Private Sub BuildLayout(visible As List(Of SettingsSection))
        ' Fill control first, edge control after: the later-added edge docks first.
        _host.Dock = DockStyle.Fill
        Content.Controls.Add(_host)
        If visible.Count = 0 Then
            _host.Controls.Add(New Label With {
                .Text = "No settings are available for your role or plan.",
                .Dock = DockStyle.Top,
                .AutoSize = True,
                .Padding = New Padding(Theme.Space2)})
            Return
        End If
        _list.Dock = DockStyle.Left
        _list.Width = ListWidth
        _list.IntegralHeight = False
        _list.DisplayMember = NameOf(SettingsSection.Title)
        _list.Items.AddRange(visible.Cast(Of Object)().ToArray())
        Content.Controls.Add(_list)
        AddHandler _list.SelectedIndexChanged, AddressOf OnSectionChanged
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        If _list.Items.Count > 0 AndAlso _list.SelectedIndex < 0 Then
            _list.SelectedIndex = 0
        End If
    End Sub

    Private Sub OnSectionChanged(sender As Object, e As EventArgs)
        CloseCurrent()
        Dim section As SettingsSection = TryCast(_list.SelectedItem, SettingsSection)
        If section Is Nothing Then
            Return
        End If
        Dim child As Form = section.OpenScreen.Invoke()
        If child Is Nothing Then
            Return
        End If
        child.TopLevel = False
        child.FormBorderStyle = FormBorderStyle.None
        child.Dock = DockStyle.Fill
        AddHandler child.FormClosed, AddressOf OnChildClosed
        _host.Controls.Add(child)
        _current = child
        child.Show()
    End Sub

    ''' <summary>The setting's own screen closed itself (its Close button): free the list so the same setting can be chosen again.</summary>
    Private Sub OnChildClosed(sender As Object, e As FormClosedEventArgs)
        If Not Object.ReferenceEquals(sender, _current) Then
            Return
        End If
        _current = Nothing
        _list.ClearSelected()
    End Sub

    Private Sub CloseCurrent()
        If _current Is Nothing Then
            Return
        End If
        Dim old As Form = _current
        _current = Nothing
        RemoveHandler old.FormClosed, AddressOf OnChildClosed
        _host.Controls.Remove(old)
        old.Dispose()
    End Sub
End Class
