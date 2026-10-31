Imports System.Windows.Forms

''' <summary>
''' Builds the module menus for the shell from the registered modules, showing only what the user may open (ADR 0016).
''' A MenuEntry group is a path: "Master|Company|Factory / Units" is Master > Company > Factory / Units.
''' </summary>
Public NotInheritable Class ShellMenuBuilder
    Private Const PathSeparator As Char = "|"c

    Private Sub New()
    End Sub

    ''' <summary>Throws InvalidOperationException when two entries share a key: that is a programming error found at start-up.</summary>
    Public Shared Function Build(modules As IEnumerable(Of IShellModule), permissions As IPermissionService,
                                 entitlements As IEntitlementService, onOpen As Action(Of ShellItem),
                                 Optional iconFor As Func(Of String, AppIcon?) = Nothing,
                                 Optional deviceDpi As Integer = 96) As IReadOnlyList(Of ToolStripMenuItem)
        If modules Is Nothing Then Throw New ArgumentNullException(NameOf(modules))
        If onOpen Is Nothing Then Throw New ArgumentNullException(NameOf(onOpen))
        ' A menu item cannot report its own dpi (ToolStripItem.DeviceDpi is internal), so the caller passes its form's DeviceDpi.
        Dim imageFor As Func(Of String, System.Drawing.Image) = Nothing
        If iconFor IsNot Nothing Then
            imageFor = Function(key As String)
                           Dim picked As AppIcon? = iconFor(key)
                           Return If(picked.HasValue, MenuImage(picked.Value, deviceDpi), Nothing)
                       End Function
        End If
        Dim items = modules.SelectMany(Function(m) m.Items).ToList()
        RejectDuplicateKeys(items)
        Dim byKey = items.ToDictionary(Function(item) item.Entry.EntryKey, StringComparer.Ordinal)
        Dim visible = MenuFilter.Visible(items.Select(Function(item) item.Entry), permissions, entitlements)
        Dim roots As New List(Of ToolStripMenuItem)()
        Dim nodes As New Dictionary(Of String, ToolStripMenuItem)(StringComparer.Ordinal)
        For Each entry In visible
            Dim target = FindOrCreateGroup(entry.MenuGroup, roots, nodes, imageFor)
            target.DropDownItems.Add(CreateItem(byKey(entry.EntryKey), onOpen, imageFor))
        Next
        Return roots
    End Function

    Private Shared Function FindOrCreateGroup(path As String, roots As List(Of ToolStripMenuItem),
                                              nodes As Dictionary(Of String, ToolStripMenuItem),
                                              imageFor As Func(Of String, System.Drawing.Image)) As ToolStripMenuItem
        Dim parent As ToolStripMenuItem = Nothing
        Dim key As String = String.Empty
        For Each part In path.Split(PathSeparator)
            Dim name = part.Trim()
            key = If(key.Length = 0, name, key & PathSeparator & name)
            Dim node As ToolStripMenuItem = Nothing
            If Not nodes.TryGetValue(key, node) Then
                node = New ToolStripMenuItem(name)
                ApplyIcon(node, key, imageFor)
                nodes.Add(key, node)
                If parent Is Nothing Then
                    roots.Add(node)
                Else
                    parent.DropDownItems.Add(node)
                End If
            End If
            parent = node
        Next
        Return parent
    End Function

    Private Shared Function CreateItem(item As ShellItem, onOpen As Action(Of ShellItem), imageFor As Func(Of String, System.Drawing.Image)) As ToolStripMenuItem
        Dim menuItem As New ToolStripMenuItem(item.Entry.Title) With {.Tag = item}
        ApplyIcon(menuItem, item.Entry.EntryKey, imageFor)
        If item.IsPlanned Then
            menuItem.Enabled = False
            menuItem.ToolTipText = "Planned: not available yet."
            Return menuItem
        End If
        AddHandler menuItem.Click, Sub(sender As Object, args As EventArgs) onOpen(CType(CType(sender, ToolStripMenuItem).Tag, ShellItem))
        Return menuItem
    End Function

    ''' <summary>The 16 dip menu picture for an icon, in the accent colour and sharp at the given dpi. Shared bitmap: do not dispose it.</summary>
    Public Shared Function MenuImage(icon As AppIcon, deviceDpi As Integer) As System.Drawing.Image
        Return IconProvider.GetBitmap(icon, 16, deviceDpi, Theme.Accent)
    End Function

    ''' <summary>Gives a menu item its picture when the lookup has one for the key (an entry key, or a group path such as Master|Company). No picture: text only.</summary>
    Private Shared Sub ApplyIcon(menuItem As ToolStripMenuItem, key As String, imageFor As Func(Of String, System.Drawing.Image))
        If imageFor Is Nothing Then
            Return
        End If
        Dim picture As System.Drawing.Image = imageFor(key)
        If picture IsNot Nothing Then
            menuItem.Image = picture
        End If
    End Sub

    Private Shared Sub RejectDuplicateKeys(items As List(Of ShellItem))
        Dim duplicate = items.GroupBy(Function(item) item.Entry.EntryKey, StringComparer.Ordinal).FirstOrDefault(Function(g) g.Count() > 1)
        If duplicate IsNot Nothing Then
            Throw New InvalidOperationException($"Two menu entries use the key '{duplicate.Key}'. Keys must be unique across modules.")
        End If
    End Sub
End Class
