Imports System.Drawing
Imports System.Windows.Forms
Imports Revolution.UI.Common

''' <summary>
''' A screen for checking the application (ADR 0020): every screen the feature modules offer, with the menu positions that open it and a button
''' to open it; every menu position with its icon, screen and rights; and every AppIcon with its code. Offered to the Developer next to the Licence screen.
''' A screen opened here is owned by this window, so it closes with it (and so with a company change, like any screen opened from the menu).
''' </summary>
Friend NotInheritable Class IconMapForm
    Inherits BaseForm

    Private ReadOnly _menuItems As Func(Of IReadOnlyList(Of ShellItem))
    Private ReadOnly _screens As Func(Of IReadOnlyList(Of ShellItem))
    Private ReadOnly _permissions As IPermissionService
    Private _screenList As ListView

    Public Sub New(menuItems As Func(Of IReadOnlyList(Of ShellItem)), screens As Func(Of IReadOnlyList(Of ShellItem)),
                   permissions As IPermissionService)
        MyBase.New("Icon and menu map", "For checking: every screen, every menu position and every icon")
        If menuItems Is Nothing Then Throw New ArgumentNullException(NameOf(menuItems))
        If screens Is Nothing Then Throw New ArgumentNullException(NameOf(screens))
        If permissions Is Nothing Then Throw New ArgumentNullException(NameOf(permissions))
        _menuItems = menuItems
        _screens = screens
        _permissions = permissions
        ClientSize = New Size(980, 640)
        Dim tabs As New TabControl With {.Dock = DockStyle.Fill}
        Dim screenSummary As String = String.Empty
        tabs.TabPages.Add(BuildScreensPage(screenSummary))
        Dim menuSummary As String = String.Empty
        tabs.TabPages.Add(BuildMenuPage(menuSummary))
        Dim iconCount As Integer = 0
        tabs.TabPages.Add(BuildIconsPage(iconCount))
        Content.Controls.Add(tabs)
        AddOpenButton()
        ShowStatus($"{screenSummary} {menuSummary} {iconCount} icons.")
    End Sub

    Private Sub AddOpenButton()
        Dim openButton As New ToolStripButton("Open selected screen") With {
            .Image = ShellMenuBuilder.MenuImage(AppIcon.Forward, DeviceDpi),
            .DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
        }
        AddHandler openButton.Click, Sub(sender As Object, args As EventArgs) OpenSelectedScreen()
        Toolbar.Items.Add(openButton)
    End Sub

    ''' <summary>Opens the selected screen the way the shell does: View right needed, shown owned by this window.</summary>
    Private Sub OpenSelectedScreen()
        If _screenList.SelectedItems.Count = 0 Then
            ShowStatus("Select a screen on the Screens tab first.")
            Return
        End If
        Dim chosen As ShellItem = TryCast(_screenList.SelectedItems(0).Tag, ShellItem)
        If chosen Is Nothing Then
            Return
        End If
        If Not _permissions.IsAllowed(chosen.Entry.ModuleKey, PermissionAction.View) Then
            ShowError("You do not have permission to open this screen.")
            Return
        End If
        ClearValidation()
        Dim screen As Form = chosen.OpenScreen.Invoke()
        screen.Show(Me)
    End Sub

    Private Function BuildScreensPage(ByRef summary As String) As TabPage
        Dim page As New TabPage("Screens")
        Dim list As New ListView With {
            .Dock = DockStyle.Fill,
            .View = View.Details,
            .FullRowSelect = True,
            .GridLines = True,
            .HideSelection = False,
            .MultiSelect = False
        }
        Dim scale As Double = DeviceDpi / 96.0
        list.Columns.Add("Screen", CInt(200 * scale))
        list.Columns.Add("Entry key", CInt(210 * scale))
        list.Columns.Add("Module", CInt(90 * scale))
        list.Columns.Add("Menu positions that open it", CInt(440 * scale))
        Dim layout As IReadOnlyList(Of ShellItem) = _menuItems()
        Dim offered = _screens().OrderBy(Function(item) item.Entry.EntryKey, StringComparer.Ordinal).ToList()
        Dim notInMenu As Integer = 0
        For Each offer As ShellItem In offered
            Dim placed = layout.Where(Function(position) Not position.IsPlanned AndAlso position.OpenScreen Is offer.OpenScreen) _
                               .Select(Function(position) position.Entry.MenuGroup.Replace("|", " > ") & " > " & position.Entry.Title).ToList()
            Dim row As New ListViewItem(offer.Entry.Title) With {.Tag = offer}
            row.SubItems.Add(offer.Entry.EntryKey)
            row.SubItems.Add(offer.Entry.ModuleKey.ToString())
            If placed.Count = 0 Then
                row.SubItems.Add("Not in the menu")
                row.ForeColor = Theme.Danger
                notInMenu += 1
            Else
                row.SubItems.Add(String.Join("; ", placed))
            End If
            list.Items.Add(row)
        Next
        AddHandler list.DoubleClick, Sub(sender As Object, args As EventArgs) OpenSelectedScreen()
        _screenList = list
        summary = $"{offered.Count} screens ({notInMenu} not in the menu)."
        page.Controls.Add(list)
        Return page
    End Function

    Private Function BuildIconsPage(ByRef iconCount As Integer) As TabPage
        Dim page As New TabPage("Icons")
        Dim edge As Integer = Math.Max(16, CInt(Math.Round(32 * DeviceDpi / 96.0, MidpointRounding.AwayFromZero)))
        Dim images As New ImageList With {.ColorDepth = ColorDepth.Depth32Bit, .ImageSize = New Size(edge, edge)}
        Dim list As New ListView With {
            .Dock = DockStyle.Fill,
            .View = View.Tile,
            .LargeImageList = images,
            .TileSize = New Size(CInt(Math.Round(230 * DeviceDpi / 96.0)), edge + 16),
            .HideSelection = False
        }
        list.Columns.Add("Icon")
        list.Columns.Add("Code")
        list.Columns.Add("Same glyph as")
        Dim all As AppIcon() = CType([Enum].GetValues(GetType(AppIcon)), AppIcon())
        For Each glyph As AppIcon In all
            Dim code As Integer = IconProvider.GlyphCode(glyph)
            Dim sharedWith As String = String.Join(", ", all.Where(Function(other) other <> glyph AndAlso IconProvider.GlyphCode(other) = code).Select(Function(other) other.ToString()))
            images.Images.Add(glyph.ToString(), IconProvider.GetBitmap(glyph, edge, Theme.Accent))
            Dim entry As New ListViewItem(glyph.ToString(), glyph.ToString())
            entry.SubItems.Add("U+" & code.ToString("X4", Globalization.CultureInfo.InvariantCulture))
            entry.SubItems.Add(sharedWith)
            list.Items.Add(entry)
        Next
        iconCount = all.Length
        page.Controls.Add(list)
        Return page
    End Function

    Private Function BuildMenuPage(ByRef summary As String) As TabPage
        Dim page As New TabPage("Menu positions")
        Dim edge As Integer = Math.Max(16, CInt(Math.Round(16 * DeviceDpi / 96.0, MidpointRounding.AwayFromZero)))
        Dim images As New ImageList With {.ColorDepth = ColorDepth.Depth32Bit, .ImageSize = New Size(edge, edge)}
        Dim list As New ListView With {
            .Dock = DockStyle.Fill,
            .View = View.Details,
            .FullRowSelect = True,
            .GridLines = True,
            .HideSelection = False,
            .SmallImageList = images
        }
        Dim scale As Double = DeviceDpi / 96.0
        list.Columns.Add("Menu", CInt(210 * scale))
        list.Columns.Add("Title", CInt(170 * scale))
        list.Columns.Add("Entry key", CInt(210 * scale))
        list.Columns.Add("Icon", CInt(110 * scale))
        list.Columns.Add("Screen", CInt(80 * scale))
        list.Columns.Add("Module", CInt(90 * scale))
        list.Columns.Add("Plan feature", CInt(110 * scale))
        Dim positions = _menuItems().OrderBy(Function(item) item.Entry.SortOrder).ThenBy(Function(item) item.Entry.Title, StringComparer.Ordinal).ToList()
        Dim withScreen As Integer = 0
        Dim withIcon As Integer = 0
        For Each position As ShellItem In positions
            Dim entry As MenuEntry = position.Entry
            Dim pictured As AppIcon? = MenuIconMap.IconFor(entry.EntryKey)
            Dim row As New ListViewItem(entry.MenuGroup.Replace("|", " > "))
            If pictured.HasValue Then
                If Not images.Images.ContainsKey(pictured.Value.ToString()) Then
                    images.Images.Add(pictured.Value.ToString(), IconProvider.GetBitmap(pictured.Value, edge, Theme.Accent))
                End If
                row.ImageKey = pictured.Value.ToString()
                withIcon += 1
            End If
            row.SubItems.Add(entry.Title)
            row.SubItems.Add(entry.EntryKey)
            row.SubItems.Add(If(pictured.HasValue, pictured.Value.ToString(), "(none)"))
            row.SubItems.Add(If(position.IsPlanned, "Planned", "Screen"))
            row.SubItems.Add(entry.ModuleKey.ToString())
            row.SubItems.Add(If(entry.RequiredFeature.HasValue, entry.RequiredFeature.Value.ToString(), "any"))
            If position.IsPlanned Then
                row.ForeColor = Theme.TextMuted
            Else
                withScreen += 1
            End If
            list.Items.Add(row)
        Next
        summary = $"{positions.Count} menu positions: {withScreen} with a screen, {positions.Count - withScreen} planned, {withIcon} with an icon."
        page.Controls.Add(list)
        Return page
    End Function
End Class
